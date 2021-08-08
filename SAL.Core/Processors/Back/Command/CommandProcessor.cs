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
    internal class CommandProcessor : IProcessor
    {
        private ILogger logger;
        private ILoggerProvider loggerProvider;
        private ISalLogger salLogger;
        private ISalService salService;

        private readonly ILifetimeScope container;

        private ISalClient salClient;

        private readonly Dictionary<string, CommandHandlerInfo> commandHandlers = new();
        private readonly Dictionary<string, WfmResultHandlerInfo> wfmResultHandler = new();

        private ISubscription subscription;

        public CommandProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(CommandProcessor));
            salClient = container.Resolve<ISalClient>();
            salService = container.Resolve<ISalService>();
        }

        private CommandProcessorConfig commandProcessorConfig;

        public void Start()
        {
            try
            {
                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>()
                        .Any(ts => ts.ServiceType == typeof(IWfmResultHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterWfmResultHandler);


                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommandHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonCommandHandler2)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonCommandHandler);

                var processingCommand = commandHandlers.Where(h => h.Value.IsInstanceHandler == false).ToArray();

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

                var config = configWatcher.GetSection(ConfigurationSectionNames.CommandProcessor);
                if (config != null)
                {
                    baseJsonConfig.Merge(config, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Merge });
                }

                commandProcessorConfig = baseJsonConfig.ToObject<CommandProcessorConfig>();

                var transport = container.Resolve<ITransport>();

                var subscriptionFactory = transport.CreateMessageSubscription();

                subscription = subscriptionFactory.CreateCommand(
                    commandProcessorConfig.GlobalPrefetchCount,
                    commandProcessorConfig.CommandPrefetchCount,
                    processingCommand.Select(k => new CommandInfo
                    {
                        CommandName = k.Key,
                        PrefetchCount = commandProcessorConfig.CommandProcessingSettings[k.Key].PrefetchCount
                    }).ToArray(),
                    Handler);
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

        private void RegisterWfmResultHandler(Type handlerType)
        {
            try
            {
                RegisterWfmResultHandler2(handlerType);
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
                .Where(i => i.IsAssignableTo<ICommandHandler2>() && i.IsGenericType).ToArray();

            var isInstanceHandler = handlerType.HasAttribute<SalInstanceHandlerAttribute>();

            ICommandSchemeCreator schemeCreator = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemeCreator = (ICommandSchemeCreator)container.Resolve(handlerType);

            ICommandNameResolver nameResolvert = null;
            if (handlerType.IsAssignableTo<ICommandNameResolver>())
                nameResolvert = (ICommandNameResolver)container.Resolve(handlerType);

            foreach(var handlerInterface in handlerInterfaces)
            {
                var args = handlerInterface.GetGenericArguments();
                


                var commandType = args[0];


                string commandName = null;
                var interfaceMethodInfo = handlerInterface.GetMethod("Handle");
                var handleMethod = handlerType.GetMethodByInterfaceMethodInfo(interfaceMethodInfo);
                if (handleMethod == null)
                    handleMethod = interfaceMethodInfo;

                if (nameResolvert != null)
                {
                    commandName = nameResolvert.Resolve(handlerInterface);
                    if (string.IsNullOrWhiteSpace(commandName))
                        throw new Exception($"Для {commandType.Name} в {handlerType.Name} не удаеться получить имя команды");
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

                var commandHandlerInfo = new CommandHandlerInfo
                {
                    CommandType = commandType,
                    CommandName = commandName,

                    HandlerType = handlerType,

                    HandleMethod = handleMethod,

                    IsCommon = false,
                    IsInstanceHandler = isInstanceHandler,
                    CommandSchema = schemeCreator == null ? SalSchema.Generate(commandType) : schemeCreator.GetCommandSchema(commandName),
                };


                commandHandlers.Add(commandName, commandHandlerInfo);
                logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");

                salService.AddBackCommandHandler(new API.CommandHandlerInfo
                {
                    IsCommon = commandHandlerInfo.IsCommon,
                    CommandName = commandHandlerInfo.CommandName,
                    IsInstanceHandler = commandHandlerInfo.IsInstanceHandler,
                    CommandSchema = commandHandlerInfo.CommandSchema,
                    ResultSchema = schemeCreator == null ? null : schemeCreator.GetResultSchema(commandName),
                });
            }
        }

        private void RegisterCommonCommandHandler2(Type handlerType)
        {
            var isInstanceHandler = handlerType.HasAttribute<SalInstanceHandlerAttribute>();


            ICommandSchemeCreator schemeCreator = null;
            if (handlerType.IsAssignableTo<ICommandSchemeCreator>())
                schemeCreator = (ICommandSchemeCreator)container.Resolve(handlerType);

            handlerType.GetAttributes<SalCommandNameAttribute>()
                .ForEach(a =>
                {
                    var commandName = a.Name;

                    if (commandHandlers.ContainsKey(commandName))
                        throw new Exception($"{commandName} уже имеет обработчик");

                    var commandHandlerInfo = new CommandHandlerInfo
                    {
                        CommandName = commandName,
                        HandlerType = handlerType,
                        HandleMethod = null,
                        CommandType = null,
                        IsCommon = true,
                        IsInstanceHandler = isInstanceHandler,
                        CommandSchema = null,
                    };


                    var handlerInfo =
                        new API.CommandHandlerInfo
                        {
                            IsCommon = commandHandlerInfo.IsCommon,
                            CommandName = commandHandlerInfo.CommandName,
                            IsInstanceHandler = commandHandlerInfo.IsInstanceHandler,
                        };

                    if (schemeCreator != null)
                    {
                        commandHandlerInfo.CommandSchema = schemeCreator.GetCommandSchema(commandName);
                        handlerInfo.CommandSchema = commandHandlerInfo.CommandSchema;
                        handlerInfo.ResultSchema = schemeCreator.GetResultSchema(commandName);
                    }

                    commandHandlers.Add(commandName, commandHandlerInfo);

                    logger.Info($"Для команды {commandName} добавлен уневерсальный обработчик {handlerType.Name}");

                    salService.AddBackCommandHandler(handlerInfo);
                });
        }


        private void RegisterWfmResultHandler2(Type handlerType)
        {
            var wfmResultHandlerNameAttr = handlerType.GetAttribute<WfmResultHandlerNameAttribute>();

            var wfmResultHandlerName = "default";
            if (wfmResultHandlerNameAttr != null)
                wfmResultHandlerName = wfmResultHandlerNameAttr.Name;

            if (wfmResultHandler.ContainsKey(wfmResultHandlerName))
                throw new Exception($"Результат ВФМ {wfmResultHandlerName} -> уже имеет обработчик");


            var wfmResultHandlerInfo = new WfmResultHandlerInfo
            {
                HandlerName = wfmResultHandlerName,
                HandlerType = handlerType,
                HandleMethod = typeof(IWfmResultHandler).GetMethod("Handle"),
            };


            wfmResultHandler.Add(wfmResultHandlerName, wfmResultHandlerInfo);
            logger.Info($"Для обработки результата ВФМ '{wfmResultHandlerName}' добавлен обработчик  '{handlerType.Name}'");
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
                HandlerContext.Set(HandlerTypes.Processor, "CommandProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandPayload = ExtractCommandPayload(transportMessage);
                HandlerContext.Update(commandPayload.ContextInfo);
                salLogger.LogIncoming(commandPayload);
                if (commandPayload.Descriptor.CommandName == "WFM.Result")
                    await ProcessingWfmResult(transportMessage, commandPayload);
                else
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
                logger.Error("При обработке команды произошла ошибка ", ex);
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


            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterType)
                && commandPayload.Descriptor.DestinationAdapterType != AdapterConfiguration.AdapterType)
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportTransportMessage.CorrelationId}");

            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterName)
                && commandPayload.Descriptor.DestinationAdapterName != AdapterConfiguration.AdapterName)
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportTransportMessage.CorrelationId}");


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

            HandlerContext.Update(HandlerTypes.CommandHandler, commandPayload.Descriptor.CommandName);


            if (commandHandlers.TryGetValue(commandPayload.Descriptor.CommandName, out var commandHandlerInfo))
            {
                var handlerName = commandHandlerInfo.HandlerType.Name;


                salLogger.LogHandler(commandPayload, handlerName);

                using var scope = container.BeginLifetimeScope();
                var handler = scope.Resolve(commandHandlerInfo.HandlerType);
                var executingContext = new ExecutingContext
                {
                    Scope = scope,
                    SalClient = scope.Resolve<ISalClient>(),
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

                await Validate(commandHandlerInfo, commandPayload);


                if (!commandHandlerInfo.IsCommon)
                {
                    var commandObject = commandPayload.Payload.ConvertValue(commandHandlerInfo.CommandType);


                    await (Task)commandHandlerInfo.HandleMethod.Invoke(handler, new[] { commandObject, commandContext, executingContext });
                }
                else
                {
                    if (handler is ICommonCommandHandler2 ccha)
                    {
                        await ccha.Handle(commandPayload.Payload.Clone(), commandContext, executingContext);
                    }
                    else
                    {
                        throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не являеться общим 2", properties: new { handlerType = handler.GetType().Name });
                    }
                }
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, $"Обработчик команды {commandPayload.Descriptor.CommandName} не найден");
            }
        }


        private async Task Validate(CommandHandlerInfo handlerInfo, CommandPayload commandPayload)
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

        protected virtual async Task ProcessingWfmResult(TransportMessage transportMessage, CommandPayload commandPayload)
        {
            var commandLogger = salLogger.GetLogger(commandPayload);

            if (commandPayload.Descriptor.TTL.HasValue &&
                commandPayload.Descriptor.PublishTimeStamp + commandPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandLogger.Trace("Команда - протухла");
                return;
            }


            HandlerContext.Update(HandlerTypes.CommandHandler, commandPayload.Descriptor.CommandName);

            /*
            IList<string> messages;
            if (!commandPayload.Payload.IsValid(wfmProcessResultCommandSchema, out messages))
            {
                
            }
            */

            var wfmProcessResultCommand = commandPayload.Payload.ConvertValue<WfmProcessResultCommand>();

            var wfmResultHandlerName = "default";
            if (!string.IsNullOrWhiteSpace(wfmProcessResultCommand.HandlerName))
            {
                if (wfmResultHandler.ContainsKey(wfmProcessResultCommand.HandlerName))
                    wfmResultHandlerName = wfmProcessResultCommand.HandlerName;
            }


            if (wfmResultHandler.TryGetValue(wfmResultHandlerName, out var wfmResultHandlerInfo))
            {
                HandlerContext.Update(handlerName: wfmResultHandlerInfo.HandlerName);
                salLogger.LogHandler(commandPayload, wfmResultHandlerInfo.HandlerName);

                using var scope = container.BeginLifetimeScope();
                var handler = (IWfmResultHandler)scope.Resolve(wfmResultHandlerInfo.HandlerType);

                await (Task)wfmResultHandlerInfo.HandleMethod.Invoke(handler, new object[] { wfmProcessResultCommand.ProcessResult, wfmProcessResultCommand.ProcessInfo });
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, $"Обработчик WfmResult ({wfmResultHandlerName}) не найден");
            }
        }
    }
}