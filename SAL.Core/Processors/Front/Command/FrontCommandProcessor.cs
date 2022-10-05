using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using SAL.API;
using SAL.Core.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Infrastructure;

namespace SAL.Core.Processors
{
    internal class FrontCommandProcessor : IProcessor
    {
        private ILogger logger;
        private ILoggerProvider loggerProvider;
        private ISalLogger salLogger;
        private ISalService salService;

        private readonly ILifetimeScope container;

        private ISalClient salClient;

        private readonly Dictionary<string, FrontCommandHandlerInfo> commandHandlers = new();

        private ISubscription subscription;
        
        private readonly IMetricProvider metricProvider;

        public FrontCommandProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(FrontCommandProcessor));
            salClient = container.ResolveKeyed<ISalClient>(Contour.Front);
            salService = container.Resolve<ISalService>();
            metricProvider = container.Resolve<IMetricProvider>();
        }

        private CommandProcessorConfig commandProcessorConfig;

        public void Start()
        {
            try
            {
                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(IFrontCommandHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(IFrontCommonCommandHandler2Async)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonCommandHandler);

                var processingCommand = commandHandlers.ToArray();

                var baseJsonConfig = new CommandProcessorConfig
                {
                    GlobalPrefetchCount = 1,
                    CommandPrefetchCount = 1,
                    CommandProcessingSettings = processingCommand.ToDictionary(kv => kv.Key, kv => new CommandProcessingSettings
                    {
                        PrefetchCount = 0
                    })
                }.ToJObjectSafe();


                var configWatcher = container.Resolve<IConfigWatcher>();

                var config = configWatcher.GetSection(ConfigurationSectionNames.FrontCommandProcessor);
                if (config != null)
                {
                    baseJsonConfig.Merge(config, new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Merge});
                }

                commandProcessorConfig = baseJsonConfig.ToObject<CommandProcessorConfig>();

                var transport = container.ResolveKeyed<ITransport>(Contour.Front);

                var subscriptionFactory = transport.CreateMessageSubscription();


                subscription = subscriptionFactory.CreateCommand(
                    commandProcessorConfig.GlobalPrefetchCount,
                    commandProcessorConfig.CommandPrefetchCount,
                    processingCommand.Select(k => new CommandInfo
                    {
                        CommandName = k.Key,
                        PrefetchCount = commandProcessorConfig.CommandProcessingSettings[k.Key].PrefetchCount
                    }).ToArray(),
                    Handler,
                    "FrontCommand");
            }
            catch (Exception ex)
            {
                logger.Error("При запуске произошла ошибка:", ex);
                throw;
            }
        }

        private void RegisterCommandHandler(Type handlerType)
        {
            try
            {
                RegisterCommandHandler2(handlerType);
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка добавления типа {handlerType.Name}", ex);
                throw;
            }
        }

        private void RegisterCommonCommandHandler(Type handlerType)
        {
            try
            {
                RegisterCommonCommandHandler2(handlerType);
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка добавления типа {handlerType.Name}", ex);
                throw;
            }
        }
        
        private void RegisterCommandHandler2(Type handlerType)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<IFrontCommandHandler2>() && i.IsGenericType).ToArray();


            ICommandSchemeCreator schemeCreator = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemeCreator = (ICommandSchemeCreator) container.Resolve(handlerType);

            ICommandNameResolver nameResolvert = null;
            if (handlerType.IsAssignableTo<ICommandNameResolver>())
                nameResolvert = (ICommandNameResolver) container.Resolve(handlerType);

            foreach(var handlerInterface in handlerInterfaces)
            {
                var interfaceMethodInfo = handlerInterface.GetMethod("Handle");
                var handleMethod = handlerType.GetMethodByInterfaceMethodInfo(interfaceMethodInfo);
                if (handleMethod == null)
                    handleMethod = interfaceMethodInfo;
                

                var args = handlerInterface.GetGenericArguments();
                var commandType = args[0];
                string commandName = null;
                if (nameResolvert != null)
                {
                    commandName = nameResolvert.Resolve(handlerInterface);
                    if (string.IsNullOrWhiteSpace(commandName))
                        throw new Exception($"Для {handlerInterface.Name} в {handlerType.Name} не удаеться получить имя команды");
                }
                else
                {
                    commandName = commandType.GetSalName();
                    
                    var cnAttr = handleMethod.GetAttribute<SalCommandNameAttribute>()?.Name;

                    if (string.IsNullOrWhiteSpace(cnAttr) && handlerInterfaces.Length == 1)
                    {
                        cnAttr = handlerType.GetAttribute<SalCommandNameAttribute>()?.Name;
                    }

                    if (string.IsNullOrWhiteSpace(cnAttr) && string.IsNullOrWhiteSpace(commandName))
                        throw new Exception($"Для {handlerInterface.Name} в {handlerType.Name} не заданно имя команды (SalCommandNameAttribute)");
                    if (!string.IsNullOrWhiteSpace(cnAttr))
                        commandName = cnAttr;
                }
                
                



                if (commandHandlers.ContainsKey(commandName))
                    throw new Exception($"Внешний метод {commandName} уже имеет обработчик");

                ICommandSchemeCreator schemaCreater = null;
                if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                    schemaCreater = (ICommandSchemeCreator) container.Resolve(handlerType);


                var commandHandlerInfo = new FrontCommandHandlerInfo
                {
                    CommandName = commandName,
                    CommandType = commandType,

                    HandlerType = handlerType,
                    IsCommon = false,

                    HandleMethod = handleMethod,
                    CommandSchema = schemaCreater == null ? SalSchema.Generate(commandType) : schemaCreater.GetCommandSchema(commandName)
                };


                commandHandlers.Add(commandName, commandHandlerInfo);
                logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");


                salService.AddFrontCommandHandler(new API.FrontCommandHandlerInfo
                {
                    CommandName = commandHandlerInfo.CommandName,
                    CommandSchema = commandHandlerInfo.CommandSchema,
                    ResultSchema = schemaCreater?.GetResultSchema(commandName),
                });
                
                metricProvider.RegisterCommand(commandHandlerInfo.CommandName);
            }
        }

        private void RegisterCommonCommandHandler2(Type handlerType)
        {
            var commandNameAttr = handlerType.GetAttribute<SalCommandNameAttribute>();

            if (commandNameAttr == null)
                throw new Exception($"Для обработчика {handlerType.Name} не заданно Command Name. (требуеться задать SalCommandNameAttribute)");

            var commandName = commandNameAttr.Name;

            if (commandHandlers.ContainsKey(commandName))
                throw new Exception($"Команда {commandName} уже имеет обработчик");
            

            ICommandSchemeCreator schemaCreater = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemaCreater = (ICommandSchemeCreator) container.Resolve(handlerType);

            var commandHandlerInfo = new FrontCommandHandlerInfo
            {
                CommandName = commandName,
                CommandType = null,
                HandlerType = handlerType,
                IsCommon = true,

                HandleMethod = null,
            };
            if (schemaCreater != null)
                commandHandlerInfo.CommandSchema = schemaCreater.GetCommandSchema(commandName);

            commandHandlers.Add(commandName, commandHandlerInfo);
            logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");


            var handlerInfo = new API.FrontCommandHandlerInfo
            {
                CommandName = commandHandlerInfo.CommandName,
            };

            if (schemaCreater != null)
            {
                handlerInfo.CommandSchema = commandHandlerInfo.CommandSchema;
                handlerInfo.ResultSchema = schemaCreater.GetResultSchema(commandName);
            }


            salService.AddFrontCommandHandler(handlerInfo);
            metricProvider.RegisterCommand(handlerInfo.CommandName);
        }


        public void Online()
        {
            subscription.Start();
        }

        public void Offline()
        {
            subscription.Stop();
        }

        public void Stop()
        {
            subscription.Stop();
        }

        private async Task Handler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            var sw = new Stopwatch();
            var commandName = "";
            var isFail = false;
            sw.Start();
            
            try
            {
                HandlerContext.Set(HandlerTypes.Processor, "FrontCommandProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandPayload = ExtractCommandPayload(transportMessage, rabbitMessage.CorrelationId);
                commandName = commandPayload.Context.Descriptor.CommandName;
                HandlerContext.Update(commandPayload.Context.ContextInfo);
                salLogger.LogIncoming(commandPayload);
                await Processing(commandPayload);
                ack();

            }
            catch (JsonReaderException ex)
            {
                var dto = ex.ToDto();
                logger.Error("При обработке команды произошла ошибка десериализации", ex);
                var sb = new StringBuilder();
                sb.AppendLine("Rabbit message Payload");
                sb.AppendLine(SalEncoding.GetString(rabbitMessage.Payload));
                logger.Info(sb.ToString());
                nack();
                isFail = true;
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
            catch (SalException ex)
            {
                nack();
                isFail = true;
                logger.Error("При обработке результата команды произошла ошибка", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                isFail = true;
                if (ex.InnerException is SalException sex)
                {
                    logger.Error("При обработке команды произошла ошибка", ex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
                }
                else
                {
                    var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                        "При обработке команды произошла ошибка"
                        , innerException: ex.InnerException
                        , properties: new
                        {
                            rabbitMessage.CorrelationId,
                            rabbitMessage.QueueName
                        });
                    logger.Error(dto, ex.InnerException);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
                }
            }
            catch (Exception ex)
            {
                nack();
                isFail = true;
                var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                    "При обработке команды произошла ошибка"
                    , innerException: ex
                    , properties: new
                    {
                        rabbitMessage.CorrelationId,
                        rabbitMessage.QueueName
                    });
                logger.Error(dto, ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
            
            sw.Stop();
            metricProvider.IncCommand(commandName, sw.Elapsed, isFail);
        }

        protected TransportMessage ExtractMessage(RabbitMessage rabbitMessage)
        {
            var transportMessage = SalSerializer.BinaryDeserialize<TransportMessage>(rabbitMessage.Payload);
            if (transportMessage == null)
                throw new Exception($"Неудалось десерилизовать сообщение | CorrelationId:{rabbitMessage.CorrelationId}");
            if (transportMessage.Type != MessageTypes.Command)
                throw new Exception($"Сообщение не команда ({transportMessage.Type}) | CorrelationId:{rabbitMessage.CorrelationId}");

            if (transportMessage.Payload == null)
                throw new Exception($"Отсутствует message.Payload | CorrelationId:{rabbitMessage.CorrelationId}");

            if (transportMessage.Payload.Type == JTokenType.Null)
                throw new Exception($"Отсутствует message.Payload | CorrelationId:{rabbitMessage.CorrelationId}");

            return transportMessage;
        }

        protected CommandPayload ExtractCommandPayload(TransportMessage transportTransportMessage, string cid)
        {
            var commandPayload = transportTransportMessage.Payload.ConvertValue<CommandPayload>();

            if (commandPayload.Context == null)
                throw new Exception($"Отсутствует CommandPayload.Context | CorrelationId:{cid}");
            
            if (commandPayload.Context.Descriptor == null)
                throw new Exception($"Отсутствует CommandPayload.Context.Descriptor | CorrelationId:{cid}");

            if (string.IsNullOrWhiteSpace(commandPayload.Context.Descriptor.CommandName))
                throw new Exception($"Пустой CommandPayload.Context.Descriptor.CommandName | CorrelationId:{cid}");

            if (commandPayload.Payload == null)
                throw new Exception($"Отсутствует CommandPayload.Payload | CorrelationId:{cid}");
            
            commandPayload.Context.Descriptor.HandlerTimeStamp = DateTime.UtcNow;

            return commandPayload;
        }

        protected virtual async Task Processing(CommandPayload commandPayload)
        {
            var commandLogger = salLogger.GetLogger(commandPayload);

            if (commandPayload.Context.Descriptor.TTL.HasValue &&
                commandPayload.Context.Descriptor.PublishTimeStamp + commandPayload.Context.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandLogger.Trace("Команда - протухла");
                return;
            }


            HandlerContext.Update(HandlerTypes.FrontCommandHandler, commandPayload.Context.Descriptor.CommandName);


            if (commandHandlers.TryGetValue(commandPayload.Context.Descriptor.CommandName, out var commandHandlerInfo))
            {
                HandlerContext.UpdateHandlerName(handlerName: commandHandlerInfo.HandlerType.Name);
                salLogger.LogHandler(commandPayload, commandHandlerInfo.HandlerType.Name);

                using var scope = container.BeginLifetimeScope();

                var executingContext = new ExecutingContext
                {
                    Scope = scope,
                    SalClient = scope.ResolveKeyed<ISalClient>(Contour.Front),
                    Logger = commandLogger
                };

                var commandContext = commandPayload.Context;
                
                var handler = scope.Resolve(commandHandlerInfo.HandlerType);

                if(!await Validate(commandHandlerInfo, commandPayload))
                    return;


                if (!commandHandlerInfo.IsCommon)
                {
                    var commandObject = commandPayload.Payload.ConvertValue(commandHandlerInfo.CommandType);
                    await (Task) commandHandlerInfo.HandleMethod.Invoke(handler, new[] {commandObject, commandContext, executingContext});
                }
                else
                {
                    if (handler is IFrontCommonCommandHandler2Async fccha)
                    {
                        await fccha.Handle(commandPayload.Payload.Clone(), commandContext, executingContext);
                    }
                    else
                    {
                        throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не являеться общим 2", properties: new {handlerType = handler.GetType().Name});
                    }
                }
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик команды не найден");
            }
        }

        private async Task<bool> Validate(FrontCommandHandlerInfo handlerInfo, CommandPayload commandPayload)
        {
            if (handlerInfo.CommandSchema != null)
            {
                IList<string> messages;
                if (!commandPayload.Payload.IsValid(handlerInfo.CommandSchema, out messages))
                {
                    var validationError = SalError.CreateValidationDto(messages.Select(m => new FieldError
                    {
                        Description = m,
                        Path = String.Empty
                    }));
                    await salClient.PublishResultAsync(validationError, commandPayload.Context);
                    return false;
                }
            }
            return true;
        }
    }
}