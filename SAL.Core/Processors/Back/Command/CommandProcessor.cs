using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Command;
using SAL.API.Monad;
using SAL.Core.Config;
using SAL.Core.DTO.Transport;
using SAL.Core.Helpers;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Core.Validators;
using SAL.Infrastructure;
using SessionManager = SAL.API.SessionManager;

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

        private readonly IDictionary<string, CommandHandlerInfo> commandHandlers = new Dictionary<string, CommandHandlerInfo>();
        private readonly IDictionary<string, WfmResultHandlerInfo> wfmResultHandler = new Dictionary<string, WfmResultHandlerInfo>();

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
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommandHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonCommandHandler)))
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
                    baseJsonConfig.Merge(config, new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Merge});
                }

                commandProcessorConfig = baseJsonConfig.ToObject<CommandProcessorConfig>();

                var configStr = commandProcessorConfig.ToIndentedJson();
                File.WriteAllText(Path.Combine(AdapterConfiguration.ConfigPath, $"{ConfigurationSectionNames.CommandProcessor}.txt"), configStr);

                logger.Info($"Command processing config \n{configStr}");

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
            var genericValidator = typeof(IValidator<>);

            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<ICommandHandler>() && i.IsGenericType).ToArray();

            var isInstanceHandler = handlerType.GetCustomAttributes(typeof(SalInstanceHandlerAttribute)).Any();

            foreach(var handlerInterface in handlerInterfaces)
            {
                var commandType = handlerInterface.GetGenericArguments()[0];

                var commandName = commandType.GetRouteKey();

                if (commandHandlers.ContainsKey(commandName))
                    throw new Exception($"{commandName} уже имеет обработчик");


                var commandHandlerInfo = new CommandHandlerInfo
                {
                    CommandType = commandType,
                    CommandName = commandName,

                    ResultType = handlerInterface.GetGenericArguments()[1],

                    HandlerType = handlerType,

                    HandlerMethod = handlerInterface.GetMethod("Handle"),
                    ValidationMethod = null,

                    IsCommon = false,
                    IsInstanceHandler = isInstanceHandler
                };


                var validator = genericValidator.MakeGenericType(commandType);

                if (handlerType.GetInterfaces().Any(i => i == validator))
                {
                    commandHandlerInfo.ValidationMethod = validator.GetMethod("Validate");
                }

                commandHandlers.Add(commandName, commandHandlerInfo);
                logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");


                if (!commandHandlerInfo.CommandName.StartsWith("System."))
                {
                    var dtos = new List<DtoInfo>();
                    dtos.AddRange(commandHandlerInfo.CommandType.GetDtoInfos());
                    dtos.AddRange(commandHandlerInfo.ResultType.GetDtoInfos());


                    salService.AddBackCommandHandler(new API.CommandHandlerInfo
                    {
                        IsCommon = commandHandlerInfo.IsCommon,
                        CommandName = commandHandlerInfo.CommandName,
                        IsInstanceHandler = commandHandlerInfo.IsInstanceHandler,
                        CommandDto = commandHandlerInfo.CommandType.Name,
                        ResultDto = commandHandlerInfo.ResultType.Name,
                        Dtos = dtos.ToArray()
                    });
                }
            }
        }

        private void RegisterCommonCommandHandler(Type handlerType)
        {
            var isInstanceHandler = handlerType.GetCustomAttributes(typeof(SalInstanceHandlerAttribute)).Any();

            ICommandDtoCreator dtoCreater = null;
            if (handlerType.IsAssignableTo<ICommandDtoCreator>())
                dtoCreater = (ICommandDtoCreator)container.Resolve(handlerType);


            handlerType.GetCustomAttributes(typeof(SalCommandHandlerAttribute))
                .OfType<SalCommandHandlerAttribute>().ForEach(a =>
                {
                    var name = a.Name;
                    name = Regex.Replace(name, "(.+)command$", "$1", RegexOptions.IgnoreCase);
                    var commandName = $"{a.ServiceType}.{name}";

                    if (commandHandlers.ContainsKey(commandName))
                        throw new Exception($"{commandName} уже имеет обработчик");

                    var commandHandlerInfo = new CommandHandlerInfo
                    {
                        CommandName = commandName,
                        HandlerType = handlerType,
                        ResultType = null,
                        HandlerMethod = null,
                        ValidationMethod = null,
                        CommandType = null,
                        IsCommon = true,
                        IsInstanceHandler = isInstanceHandler
                    };

                    commandHandlers.Add(commandName, commandHandlerInfo);

                    logger.Info($"Для команды {commandName} добавлен уневерсальный обработчик {handlerType.Name}");


                    var handlerInfo =
                        new API.CommandHandlerInfo
                        {
                            IsCommon = commandHandlerInfo.IsCommon,
                            CommandName = commandHandlerInfo.CommandName,
                            IsInstanceHandler = commandHandlerInfo.IsInstanceHandler,
                        };

                    if (dtoCreater != null)
                    {
                        handlerInfo.Dtos = dtoCreater.GetCommandDtos(commandName);
                        handlerInfo.CommandDto = dtoCreater.GetCommandDtoName(commandName);
                        handlerInfo.ResultDto = dtoCreater.GetResultDtoName(commandName);
                    }



                    salService.AddBackCommandHandler(handlerInfo);
                });
        }


        private void RegisterWfmResultHandler(Type handlerType)
        {
            var wfmResultHandlerNameAttr = handlerType.GetCustomAttributes(typeof(WfmResultHandlerNameAttribute)).OfType<WfmResultHandlerNameAttribute>().FirstOrDefault();

            var wfmResultHandlerName = "default";
            if (wfmResultHandlerNameAttr != null)
                wfmResultHandlerName = wfmResultHandlerNameAttr.Name;

            if (wfmResultHandler.ContainsKey(wfmResultHandlerName))
                throw new Exception($"Результат ВФМ {wfmResultHandlerName} -> уже имеет обработчик");


            var wfmResultHandlerInfo = new WfmResultHandlerInfo
            {
                HandlerName = wfmResultHandlerName,
                HandlerType = handlerType,
                HandlerMethod = typeof(IWfmResultHandler).GetMethod("Handle"),
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
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = "CommandProcessor";

                var transportMessage = ExtractMessage(rabbitMessage);
                var commandPayload = ExtractCommandPayload(transportMessage);
                SessionManager.StartAdapterSession(transportMessage.Session);
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

        protected Message ExtractMessage(RabbitMessage rabbitMessage)
        {
            var transportMessage = SalSerializer.BinaryDeserialize<Message>(rabbitMessage.Payload);
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

        protected CommandPayload ExtractCommandPayload(Message transportMessage)
        {
            var commandPayload = transportMessage.Payload.ConvertValue<CommandPayload>();

            if (commandPayload.Descriptor == null)
                throw new Exception($"Отсутствует commandPayload.Descriptor | CorrelationId:{transportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(commandPayload.Descriptor.CommandName))
                throw new Exception($"Пустой commandPayload.Descriptor.CommandName | CorrelationId:{transportMessage.CorrelationId}");

            if (commandPayload.Payload == null)
                throw new Exception($"Отсутствует commandPayload.Payload | CorrelationId:{transportMessage.CorrelationId}");


            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterType)
                && commandPayload.Descriptor.DestinationAdapterType != AdapterConfiguration.AdapterType)
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportMessage.CorrelationId}");

            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterName)
                && commandPayload.Descriptor.DestinationAdapterName != AdapterConfiguration.AdapterName)
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportMessage.CorrelationId}");


            commandPayload.Descriptor.HandlerTimeStamp = DateTime.UtcNow;

            return commandPayload;
        }

        protected virtual async Task Processing(Message message, CommandPayload commandPayload)
        {
            var commandLogger = salLogger.GetLogger(commandPayload);

            if (commandPayload.Descriptor.TTL.HasValue &&
                commandPayload.Descriptor.PublishTimeStamp + commandPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandLogger.Trace("Команда - протухла");
                return;
            }


            HandlerContext.Type = HandlerTypes.CommandHandler;
            HandlerContext.Name = commandPayload.Descriptor.CommandName;

            if (commandHandlers.TryGetValue(commandPayload.Descriptor.CommandName, out var commandHandlerInfo))
            {
                HandlerContext.Name = commandHandlerInfo.HandlerType.Name;

                salLogger.LogHandler(commandPayload, HandlerContext.Name);

                using var scope = container.BeginLifetimeScope();
                var handler = (ICommandHandler) scope.Resolve(commandHandlerInfo.HandlerType);
                var executingContext = new ExecutingContext
                {
                    Scope = scope,
                    SalClient = scope.Resolve<ISalClient>(),
                    Logger = commandLogger
                };

                var commandContext = new CommandContext
                {
                    Descriptor = commandPayload.Descriptor,
                    Session = message.Session.DeepClone() as JObject,
                };

                handler.SetContexts(commandContext, executingContext);

                if (!commandHandlerInfo.IsCommon)
                {
                    var commandObject = commandPayload.Payload.ConvertValue(commandHandlerInfo.CommandType);
                    var validator = scope.Resolve<ObjectValidator>();

                    var validationErrors = await validator.ValidateData(commandObject,
                        o => Validate(handler, commandHandlerInfo, o));

                    if (validationErrors.Any())
                    {
                        await salClient.PublishResultAsync(validationErrors, commandPayload.Descriptor);
                        return;
                    }

                    await (Task) commandHandlerInfo.HandlerMethod.Invoke(handler, new[] {commandObject});
                }
                else
                {
                    await ExecuteCommonHandlerAsync(handler, commandPayload.Payload);
                }
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, $"Обработчик команды {commandPayload.Descriptor.CommandName} не найден");
            }
        }

        protected virtual async Task ProcessingWfmResult(Message message, CommandPayload commandPayload)
        {
            var commandLogger = salLogger.GetLogger(commandPayload);

            if (commandPayload.Descriptor.TTL.HasValue &&
                commandPayload.Descriptor.PublishTimeStamp + commandPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                commandLogger.Trace("Команда - протухла");
                return;
            }


            HandlerContext.Type = HandlerTypes.CommandHandler;
            HandlerContext.Name = commandPayload.Descriptor.CommandName;

            var wfmProcessResultCommand = commandPayload.Payload.ConvertValue<WfmProcessResultCommand>();

            var wfmResultHandlerName = "default";
            if (!string.IsNullOrWhiteSpace(wfmProcessResultCommand.HandlerName))
            {
                if (wfmResultHandler.ContainsKey(wfmProcessResultCommand.HandlerName))
                    wfmResultHandlerName = wfmProcessResultCommand.HandlerName;
            }


            if (wfmResultHandler.TryGetValue(wfmResultHandlerName, out var wfmResultHandlerInfo))
            {
                HandlerContext.Name = wfmResultHandlerInfo.HandlerName;
                salLogger.LogHandler(commandPayload, HandlerContext.Name);

                using var scope = container.BeginLifetimeScope();
                var handler = (IWfmResultHandler) scope.Resolve(wfmResultHandlerInfo.HandlerType);

                await (Task) wfmResultHandlerInfo.HandlerMethod.Invoke(handler, new object[] {wfmProcessResultCommand.ProcessResult, wfmProcessResultCommand.ProcessInfo});
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, $"Обработчик WfmResult ({wfmResultHandlerName}) не найден");
            }
        }


        private Task<IEnumerable<FieldError>> Validate(object handler, CommandHandlerInfo handlerInfo, object validateObject)
        {
            if (handlerInfo.ValidationMethod == null)
                return Task.FromResult(new List<FieldError>().AsEnumerable());

            return (Task<IEnumerable<FieldError>>) handlerInfo.ValidationMethod.Invoke(handler, new[] {validateObject});
        }

        private Task ExecuteCommonHandlerAsync(object handler, JObject command)
        {
            if (handler is ICommonCommandHandler ccha)
            {
                return ccha.Handle(command);
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не являеться общим", properties: new {handlerType = handler.GetType().Name});
            }
        }
    }
}