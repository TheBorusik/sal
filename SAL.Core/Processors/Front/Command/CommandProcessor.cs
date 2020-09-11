using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Client;
using SAL.API.Command;
using SAL.API.FrontCommand;
using SAL.API.LoggerHelper;
using SAL.API.Monad;
using SAL.Core.Config;
using SAL.Core.DTO.Transport;
using SAL.Core.Helpers;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Core.Validators;
using SAL.Infrastructure;
using SAL.Infrastructure.FrontAttributes;
using SessionManager = SAL.API.SessionManager;

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

        private readonly IDictionary<string, FrontCommandHandlerInfo> commandHandlers = new Dictionary<string, FrontCommandHandlerInfo>();

        private ISubscription subscription;

        public FrontCommandProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(CommandProcessor));
            salClient = container.ResolveNamed<ISalClient>("front");
            salService = container.Resolve<ISalService>();
        }

        private CommandProcessorConfig commandProcessorConfig;


        public void Start()
        {
            try
            {
                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(IFrontCommandHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommandHandler);


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

                var configStr = commandProcessorConfig.ToIndentedJson();
                File.WriteAllText(Path.Combine(AdapterConfiguration.ConfigPath, $"{ConfigurationSectionNames.FrontCommandProcessor}.txt"), configStr);

                logger.Info($"Command processing config \n{configStr}");

                var transport = container.ResolveNamed<ITransport>("front");

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
            var genericValidator = typeof(IValidator<>);

            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<IFrontCommandHandler>() && i.IsGenericType).ToArray();


            if (handlerInterfaces.Length == 0)
                return;

            if (handlerInterfaces.Length > 1)
                throw new Exception($"Для обработчика {handlerType.Name} заданно больше чем один обрабатываемый внешний метод");


            var externalServiceMethod = handlerType.GetCustomAttributes(typeof(SalExternalMethodAttribute))
                .OfType<SalExternalMethodAttribute>().FirstOrDefault();

            if (externalServiceMethod == null)
                throw new Exception($"Для обработчика {handlerType.Name} не заданно имя внешнего метода.");


            var externalUris = handlerType.GetCustomAttributes(typeof(SalExternalUriAttribute))
                .OfType<SalExternalUriAttribute>().Select(a => a.Uri).ToArray();


            foreach (var handlerInterface in handlerInterfaces)
            {
                var commandType = handlerInterface.GetGenericArguments()[0];

                var commandName = externalServiceMethod.ServiceMethod;

                if (commandHandlers.ContainsKey(commandName))
                    throw new Exception($"метод {commandName} уже имеет обработчик");


                var commandHandlerInfo = new FrontCommandHandlerInfo
                {
                    CommandName = commandName,
                    CommandType = commandType,
                    ResultType = handlerInterface.GetGenericArguments()[1],

                    HandlerType = handlerType,

                    HandlerMethod = handlerInterface.GetMethod("Handle"),
                    ValidationMethod = null,
                };


                var validator = genericValidator.MakeGenericType(commandType);

                if (handlerType.GetInterfaces().Any(i => i == validator))
                {
                    commandHandlerInfo.ValidationMethod = validator.GetMethod("Validate");
                }

                commandHandlers.Add(commandName, commandHandlerInfo);
                logger.Info($"Для команды {commandName} добавлен обработчик {handlerType.Name}");

                var dtos = new List<DtoInfo>();
                dtos.AddRange(commandHandlerInfo.CommandType.GetDtoInfos());
                dtos.AddRange(commandHandlerInfo.ResultType.GetDtoInfos());

                salService.AddFrontCommandHandler(new API.FrontCommandHandlerInfo
                {
                    CommandName = commandHandlerInfo.CommandName,
                    CommandDto = commandHandlerInfo.CommandType.Name,
                    ResultDto = commandHandlerInfo.ResultType.Name,
                    Dtos = dtos.ToArray(),
                    ExternalMethod = externalServiceMethod.ServiceMethod,
                    ExternalUri = externalUris
                });
            }
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
                var transportMessage = await ExtractMessage(rabbitMessage);
                var commandPayload = await ExtractCommandPayload(transportMessage);
                SessionManager.StartAdapterSession(transportMessage.Session);
                salLogger.LogIncoming(commandPayload);
                await Processing(transportMessage, commandPayload);
                ack();
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

        protected Task<Message> ExtractMessage(RabbitMessage rabbitMessage)
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

            return Task.FromResult(transportMessage);
        }

        protected Task<CommandPayload> ExtractCommandPayload(Message transportMessage)
        {
            var commandPayload = transportMessage.Payload.ConvertValue<CommandPayload>();

            if (commandPayload.Descriptor == null)
                throw new Exception($"Отсутствует commandPayload.Descriptor | CorrelationId:{transportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(commandPayload.Descriptor.CommandName))
                throw new Exception($"Пустой commandPayload.Descriptor.CommandName | CorrelationId:{transportMessage.CorrelationId}");

            if (commandPayload.Payload == null)
                throw new Exception($"Отсутствует commandPayload.Payload | CorrelationId:{transportMessage.CorrelationId}");


            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterType) &&
                !string.Equals(commandPayload.Descriptor.DestinationAdapterType, AdapterConfiguration.AdapterType, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportMessage.CorrelationId}");

            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterName) &&
                !string.Equals(commandPayload.Descriptor.DestinationAdapterName, AdapterConfiguration.AdapterName, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterName и AdapterName для команды CorrelationId:{transportMessage.CorrelationId}");

            commandPayload.Descriptor.HandlerTimeStamp = DateTime.UtcNow;

            return Task.FromResult(commandPayload);
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
                var handler = (IFrontCommandHandler) scope.Resolve(commandHandlerInfo.HandlerType);
                var executingContext = new ExecutingContext
                {
                    Scope = scope,
                    SalClient = scope.ResolveNamed<ISalClient>("front"),
                    Logger = commandLogger
                };

                var commandContext = new CommandContext
                {
                    Descriptor = commandPayload.Descriptor,
                    Session = message.Session.DeepClone() as JObject,
                };

                handler.SetContexts(commandContext, executingContext);


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
                throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик команды не найден");
            }
        }

        private Task<IEnumerable<FieldError>> Validate(object handler, FrontCommandHandlerInfo handlerInfo, object validateObject)
        {
            if (handlerInfo.ValidationMethod == null)
                return Task.FromResult(new List<FieldError>().AsEnumerable());

            return (Task<IEnumerable<FieldError>>) handlerInfo.ValidationMethod.Invoke(handler, new[] {validateObject});
        }

    }
}