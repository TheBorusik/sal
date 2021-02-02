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
using SAL.API;
using SAL.API.FrontCommand;
using SAL.API.Monad;
using SAL.Core.Config;
using SAL.Core.DTO.Transport;
using SAL.Core.Rabbit;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;


namespace SAL.Core.Processors
{
    internal class FrontExternalHttpProcessor : IProcessor
    {
        private ILogger logger;
        private ILoggerProvider loggerProvider;
        private ISalLogger salLogger;
        private ISalService salService;

        private readonly ILifetimeScope container;

        private ISalClient salClient;

        private readonly IDictionary<string, FrontExternalHttpHandlerInfo> handlers = new Dictionary<string, FrontExternalHttpHandlerInfo>();

        private ISubscription subscription;

        public FrontExternalHttpProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(FrontExternalHttpProcessor));
            salClient = container.ResolveNamed<ISalClient>("front");
            salService = container.Resolve<ISalService>();
        }

        private ExternalHttpProcessorConfig processorConfig;


        public void Start()
        {
            try
            {
                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(IFrontExternalHttpMethod)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterHandler);

                var processingPath = handlers.ToArray();

                if (!processingPath.Any())
                    return;

                var baseJsonConfig = new ExternalHttpProcessorConfig
                {
                    GlobalPrefetchCount = 1,
                    ExternalHttpSettings = processingPath.ToDictionary(kv => kv.Key, kv => new CommandProcessingSettings
                    {
                        PrefetchCount = 0
                    })
                }.ToJObjectSafe();


                var configWatcher = container.Resolve<IConfigWatcher>();

                var config = configWatcher.GetSection(ConfigurationSectionNames.FrontExternalHttpProcessor);
                if (config != null)
                {
                    baseJsonConfig.Merge(config, new JsonMergeSettings {MergeArrayHandling = MergeArrayHandling.Merge});
                }

                processorConfig = baseJsonConfig.ToObject<ExternalHttpProcessorConfig>();

                var configStr = processorConfig.ToIndentedJson();
                File.WriteAllText(Path.Combine(AdapterConfiguration.ConfigPath, $"{ConfigurationSectionNames.FrontExternalHttpProcessor}.txt"), configStr);

                logger.Info($"Command processing config \n{configStr}");

                var transport = container.ResolveNamed<ITransport>("front");

                var subscriptionFactory = transport.CreateMessageSubscription();


                subscription = subscriptionFactory.CreateExternalHttp(
                    processorConfig.GlobalPrefetchCount,
                    processingPath.Select(k => new ExternalHttpInfo
                    {
                        Path = k.Key,
                        PrefetchCount = processorConfig.ExternalHttpSettings[k.Key].PrefetchCount
                    }).ToArray(),
                    Handler,
                    "ExternalHttp");
            }
            catch (Exception ex)
            {
                logger.Error("При запуске произошла ошибка:", ex);
                throw;
            }
        }

        private void RegisterHandler(Type handlerType)
        {
            var externalPathMethod = handlerType.GetCustomAttributes(typeof(SalExternalHttpPathAttribute))
                .OfType<SalExternalHttpPathAttribute>().FirstOrDefault();
            if (externalPathMethod == null)
                throw new Exception($"Для обработчика {handlerType.Name} не заданн внешний адрес");

            if (handlers.ContainsKey(externalPathMethod.Uri))
                throw new Exception($"Внешний адрес {externalPathMethod} уже имеет обработчик");


            var handlerInfo = new FrontExternalHttpHandlerInfo
            {
                HandlerType = handlerType,
                ExternalPath = externalPathMethod.Uri.ToLower()
            };

            handlers.Add(externalPathMethod.Uri, handlerInfo);
            logger.Info($"Для внешнего адреса {externalPathMethod.Uri} добавлен обработчик {handlerType.Name}");

            salService.AddExternalHttpHandler(externalPathMethod.Uri);
        }


        public void Online()
        {
            subscription?.Start();
        }

        public void Offline()
        {
            subscription?.Stop();
        }

        public void Stop()
        {
            subscription?.Stop();
        }

        private async Task Handler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            try
            {
                var transportMessage = ExtractMessage(rabbitMessage);
                var commandPayload = ExtractCommandPayload(transportMessage);
                SessionManager.StartAdapterSession(transportMessage.Session);
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
                logger.Error("При обработке произошла ошибка", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                if (ex.InnerException is SalException sex)
                {
                    logger.Error("При обработке произошла ошибка", ex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
                }
                else
                {
                    var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                        "При обработке произошла ошибка"
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
                    "При обработке произошла ошибка"
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


            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterType) &&
                !string.Equals(commandPayload.Descriptor.DestinationAdapterType, AdapterConfiguration.AdapterType, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterType и AdapterType для команды CorrelationId:{transportMessage.CorrelationId}");

            if (!string.IsNullOrWhiteSpace(commandPayload.Descriptor.DestinationAdapterName) &&
                !string.Equals(commandPayload.Descriptor.DestinationAdapterName, AdapterConfiguration.AdapterName, StringComparison.InvariantCultureIgnoreCase))
                throw new Exception($"Не соответствие Descriptor.DestinationAdapterName и AdapterName для команды CorrelationId:{transportMessage.CorrelationId}");

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


            var externalHttpRequest = commandPayload.Payload.ConvertValue<ExternalHttpRequest>();

            if (handlers.TryGetValue(externalHttpRequest.Path.ToLower(), out var commandHandlerInfo))
            {
                HandlerContext.Name = commandHandlerInfo.HandlerType.Name;

                salLogger.LogHandler(commandPayload, HandlerContext.Name);

                using var scope = container.BeginLifetimeScope();
                var handler = (IFrontExternalHttpMethod) scope.Resolve(commandHandlerInfo.HandlerType);

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


                await handler.Handle(externalHttpRequest, commandContext, executingContext);
            }
            else
            {
                throw SalError.CreateException(SalErrorCodes.Fatal, "Обработчик не найден");
            }
        }
    }
}