using System;
using System.Collections.Generic;
using System.IO;
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
using SAL.Core.Rabbit;
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

        public FrontCommandProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(FrontCommandProcessor));
            salClient = container.ResolveKeyed<ISalClient>(Contour.Front);
            salService = container.Resolve<ISalService>();
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

                if (!AdapterConfiguration.InDocker)
                {
                    var tmp = new JObject();
                    tmp.AddOrUpdate(ConfigurationSectionNames.FrontCommandProcessor, commandProcessorConfig);
                    var configStr = tmp.ToIndentedJson();
                    File.WriteAllText(Path.Combine(AdapterConfiguration.ConfigPath, $"{ConfigurationSectionNames.FrontCommandProcessor}.txt"), configStr);
                    logger.Info($"Front Command processing config \n{configStr}");
                }

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
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<IFrontCommandHandler2>() && i.IsGenericType).ToArray();


            if (handlerInterfaces.Length == 0)
                return;

            if (handlerInterfaces.Length > 1)
                throw new Exception($"Для обработчика {handlerType.Name} заданно больше чем один обрабатываемый внешний метод");
            
            var externalUris = handlerType.GetAttributes<SalExternalUriAttribute>().Select(a => a.Uri).ToArray();

            var handlerInterface = handlerInterfaces.First();
            
            var args = handlerInterface.GetGenericArguments();

            var commandType = args[0];
            Type resultType = null;
                
            if (args.Length == 2)
                resultType = args[1];
            
            var commandName = commandType.GetRequestType();
            if(string.IsNullOrWhiteSpace(commandName))
                throw new Exception($"Для типа {commandType.Name} не задан SalExtRequestType Attribute");
            

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

                HandlerMethod = handlerInterface.GetMethod("Handle"),
                CommandSchema = schemaCreater == null ? SalSchema.Generate(commandType) : schemaCreater.GetCommandSchema(commandName)
            };

            
            commandHandlers.Add(commandName, commandHandlerInfo);
            logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");


            salService.AddFrontCommandHandler(new API.FrontCommandHandlerInfo
            {
                CommandName = commandHandlerInfo.CommandName,
                CommandSchema = commandHandlerInfo.CommandSchema,
                ResultSchema = schemaCreater == null ? SalSchema.Generate(resultType) : schemaCreater.GetResultSchema(commandName),
                ExternalUri = externalUris
            });
        }

        private void RegisterCommonCommandHandler(Type handlerType)
        {
            var extRequestType = handlerType.GetAttribute<SalRequestTypeAttribute>();

            if (extRequestType == null)
                throw new Exception($"Для обработчика {handlerType.Name} не заданно Request Type. (требуеться задать SalRequestTypeAttribute)");
            
            var externalUris = handlerType.GetCustomAttributes(typeof(SalExternalUriAttribute))
                .OfType<SalExternalUriAttribute>().Select(a => a.Uri).ToArray();

            var commandName = extRequestType.RequestType;

            if (commandHandlers.ContainsKey(commandName))
                throw new Exception($"метод {commandName} уже имеет обработчик");

            ICommandSchemeCreator schemaCreater = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemaCreater = (ICommandSchemeCreator) container.Resolve(handlerType);

            var commandHandlerInfo = new FrontCommandHandlerInfo
            {
                CommandName = commandName,
                CommandType = null,
                HandlerType = handlerType,
                IsCommon = true,

                HandlerMethod = null,
            };
            if (schemaCreater != null)
                commandHandlerInfo.CommandSchema = schemaCreater.GetCommandSchema(commandName);

            commandHandlers.Add(commandName, commandHandlerInfo);
            logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");


            var handlerInfo = new API.FrontCommandHandlerInfo
            {
                CommandName = commandHandlerInfo.CommandName,
                ExternalUri = externalUris,
            };

            if (schemaCreater != null)
            {
                handlerInfo.CommandSchema = commandHandlerInfo.CommandSchema;
                handlerInfo.ResultSchema = schemaCreater.GetResultSchema(commandName);
            }


            salService.AddFrontCommandHandler(handlerInfo);
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
            try
            {
                HandlerContext.Set(HandlerTypes.Processor, "FrontCommandProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandPayload = ExtractCommandPayload(transportMessage);
                HandlerContext.Update(commandPayload.ContextInfo);
                salLogger.LogIncoming(commandPayload);
                await Processing(transportMessage, commandPayload);
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
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto);
            }
            catch (SalException ex)
            {
                nack();
                logger.Error("При обработке результата команды произошла ошибка", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
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

        protected CommandPayload ExtractCommandPayload(TransportMessage transportTransportMessage)
        {
            var commandPayload = transportTransportMessage.Payload.ConvertValue<CommandPayload>();

            if (commandPayload.Descriptor == null)
                throw new Exception($"Отсутствует commandPayload.Descriptor | CorrelationId:{transportTransportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(commandPayload.Descriptor.CommandName))
                throw new Exception($"Пустой commandPayload.Descriptor.CommandName | CorrelationId:{transportTransportMessage.CorrelationId}");

            if (commandPayload.Payload == null)
                throw new Exception($"Отсутствует commandPayload.Payload | CorrelationId:{transportTransportMessage.CorrelationId}");


            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterType) &&
                !string.Equals(commandPayload.Descriptor.DestinationAdapterType, AdapterConfiguration.AdapterType, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportTransportMessage.CorrelationId}");

            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterName) &&
                !string.Equals(commandPayload.Descriptor.DestinationAdapterName, AdapterConfiguration.AdapterName, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterName и AdapterName для команды CorrelationId:{transportTransportMessage.CorrelationId}");

            commandPayload.Descriptor.HandlerTimeStamp = DateTime.UtcNow;

            return commandPayload;
        }

        protected virtual async Task Processing(TransportMessage transportMessage, CommandPayload commandPayload)
        {
            var commandLogger = salLogger.GetLogger(commandPayload);

            if (commandPayload.Descriptor.TTL.HasValue &&
                commandPayload.Descriptor.PublishTimeStamp + commandPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandLogger.Trace("Команда - протухла");
                return;
            }


            HandlerContext.Update(HandlerTypes.FrontCommandHandler, commandPayload.Descriptor.CommandName);


            if (commandHandlers.TryGetValue(commandPayload.Descriptor.CommandName, out var commandHandlerInfo))
            {
                HandlerContext.Update(handlerName: commandHandlerInfo.HandlerType.Name);
                salLogger.LogHandler(commandPayload, commandHandlerInfo.HandlerType.Name);

                using var scope = container.BeginLifetimeScope();

                var executingContext = new ExecutingContext
                {
                    Scope = scope,
                    SalClient = scope.ResolveKeyed<ISalClient>(Contour.Front),
                    Logger = commandLogger
                };

                var commandContext = new CommandContext
                {
                    Descriptor = commandPayload.Descriptor,
                    ContextInfo = new ContextInfo
                    {
                        SessionId = HandlerContext.SessionId,
                        AuthId = HandlerContext.AuthId,
                        ProcessId = HandlerContext.ProcessId,
                        OperationId = HandlerContext.OperationId
                    }
                };
                
                var handler = scope.Resolve(commandHandlerInfo.HandlerType);
                
                await Validate(commandHandlerInfo, commandPayload);


                if (!commandHandlerInfo.IsCommon)
                {
                    var commandObject = commandPayload.Payload.ConvertValue(commandHandlerInfo.CommandType);
                    await (Task) commandHandlerInfo.HandlerMethod.Invoke(handler, new[] {commandObject, commandContext, executingContext});
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

        private async Task Validate(FrontCommandHandlerInfo handlerInfo, CommandPayload commandPayload)
        {
            if (handlerInfo.CommandSchema != null)
            {
                IList<string> messages;
                if (!commandPayload.Payload.IsValid(handlerInfo.CommandSchema, out messages))
                {
                    await salClient.PublishResultAsync(messages.Select(m => new FieldError
                        {
                            Description = m,
                            Path = String.Empty
                        }).ToArray(), 
                        new CommandContext
                        {
                            Descriptor = commandPayload.Descriptor,
                            ContextInfo = commandPayload.ContextInfo
                        });
                }
            }
        }
    }
}