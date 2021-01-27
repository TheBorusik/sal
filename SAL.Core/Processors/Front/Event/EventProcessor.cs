using System;
using System.Collections.Generic;
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
using SAL.API.Events;
using SAL.API.Monad;
using SAL.Core.Config;
using SAL.Core.DTO.Transport;
using SAL.Core.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Infrastructure.EventAttributes;

// ReSharper disable once CheckNamespace
namespace SAL.Core.Processors
{
    internal class FrontEventProcessor : IProcessor
    {
        private ILoggerProvider loggerProvider;
        private ILogger logger;
        private ISalLogger salLogger;
        private ISalService salService;

        private ISubscription subscription;
        private ISubscription systemSubscription;
        private ISalClient salClient;

        private readonly ILifetimeScope container;


        private readonly IDictionary<string, List<EventHandlerInfo>> eventHandlers = new Dictionary<string, List<EventHandlerInfo>>();

        public FrontEventProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger("FrontEventProcessor");
            salService = container.Resolve<ISalService>();
        }

        public void Start()
        {
            try
            {
                salClient = container.ResolveNamed<ISalClient>("front");

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(IEventHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterEventHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonEventHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonEventHandler);

                var configWatcher = container.Resolve<IConfigWatcher>();
                var jsonConfig = configWatcher.GetSection(ConfigurationSectionNames.FrontEventProcessor);
                var config = new EventProcessorConfig();

                if (jsonConfig != null)
                {
                    config = jsonConfig.ConvertValue<EventProcessorConfig>();
                }


                var transport = container.ResolveNamed<ITransport>("front");

                var subscriptionFactory = transport.CreateMessageSubscription();

                var eventList = eventHandlers.Where(eh => eh.Value.Any(h => h.IsSystem == false)).Select(eh => eh.Key).ToArray();
                var systemEventList = eventHandlers.Where(eh => eh.Value.Any(h => h.IsSystem == true)).Select(eh => eh.Key).ToArray();

                subscription = subscriptionFactory.CreateEvent(config.PrefetchCount, eventList, Handler, "FrontEvent");
                systemSubscription = subscriptionFactory.CreateSystemEvent(config.SystemPrefetchCount, systemEventList, Handler, "FrontSystemEvent");
            }
            catch (Exception ex)
            {
                logger.Error("При запуске произошла ошибка:", ex);
                throw;
            }
        }

        public void Online()
        {
            subscription.Start();
            systemSubscription.Start();
        }

        public void Offline()
        {
            subscription.Stop();
        }

        public void Stop()
        {
            subscription.Stop();
            systemSubscription.Stop();
        }

        private void RegisterEventHandler(Type handlerType)
        {
            var handlerInterfaces = handlerType.GetInterfaces()
                .Where(i => i.IsAssignableTo<IEventHandler>() && i.IsGenericType).ToArray();

            foreach (var handlerInterface in handlerInterfaces)
            {
                var eventType = handlerInterface.GetGenericArguments()[0];
                var eventName = eventType.GetRouteKey();

                var isSystem = eventType.IsSystemEvent();

                var eventHandlerInfo = new EventHandlerInfo
                {
                    HandlerType = handlerType,
                    EventName = eventName,
                    EventType = eventType,

                    IsSystem = isSystem,
                    HandlerMethod = handlerInterface.GetMethod("Handle"),
                    IsCommon = false,
                };


                if (eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers.Add(eventHandlerInfo);
                }
                else
                {
                    handlers = new List<EventHandlerInfo>();
                    handlers.Add(eventHandlerInfo);
                    eventHandlers.Add(eventName, handlers);
                    salService.AddFrontEventHandler(new API.EventHandlerInfo
                    {
                        IsSystem = eventHandlerInfo.IsSystem,
                        IsCommon = eventHandlerInfo.IsCommon,
                        EventName = eventName,
                        EventDto = eventType.Name,
                        Dtos = eventType.GetDtoInfos()
                    });
                }

                logger.Info($"Для евента {eventName} добавлен обработчик результата {handlerType.Name}");
            }
        }

        private void RegisterCommonEventHandler(Type handlerType)
        {
            var attrs = handlerType
                .GetCustomAttributes(typeof(SalEventHandlerAttribute)).OfType<SalEventHandlerAttribute>().ToArray();


            attrs.ForEach(a =>
            {
                var eventName = a.EventName;

                var eventHandlerInfo = new EventHandlerInfo
                {
                    HandlerType = handlerType,
                    EventName = eventName,
                    EventType = null,
                    IsSystem = false,
                    HandlerMethod = null,
                    IsCommon = true,
                };

                if (eventHandlers.TryGetValue(eventName, out var handlers))
                {
                    handlers.Add(eventHandlerInfo);
                }
                else
                {
                    handlers = new List<EventHandlerInfo>();
                    handlers.Add(eventHandlerInfo);
                    eventHandlers.Add(eventName, handlers);
                    salService.AddFrontEventHandler(new API.EventHandlerInfo
                    {
                        IsSystem = eventHandlerInfo.IsSystem,
                        IsCommon = eventHandlerInfo.IsCommon,
                        EventName = eventName
                    });
                }

                logger.Info($"Для евента {eventName} добавлен уневерсальный обработчик результата {handlerType.Name}");
            });
        }

        private async Task Handler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            try
            {
                HandlerContext.Type = HandlerTypes.Processor;
                HandlerContext.Name = "FrontEventProcessor";
                var transportMessage = ExtractMessage(rabbitMessage);
                var eventPayload = ExtractEventPayload(transportMessage);
                SessionManager.StartAdapterSession(transportMessage.Session);
                salLogger.LogIncoming(eventPayload);
                await Processing(transportMessage, eventPayload);
                ack();
            }
            catch (JsonReaderException ex)
            {
       
                var dto = ex.ToDto();
                logger.Error("При обработке команды произошла ошибка десериализации", ex);
                var sb = new StringBuilder();
                sb.AppendLine("Rabbit message Payload");
                sb.AppendLine(SalEncoding.GetString(rabbitMessage.Payload.Span));
                logger.Info(sb.ToString());
                nack();
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, dto); 
            }
            catch (SalException ex)
            {
                nack();
                logger.Error("При обработке event произошла ошибка", ex);
                await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
            }
            catch (TargetInvocationException ex)
            {
                nack();
                if (ex.InnerException is SalException sex)
                {
                    logger.Error("При обработке event произошла ошибка", ex);
                    await salClient.RaiseExceptionDetectEvent(rabbitMessage.CorrelationId, ex.ToDto());
                }
                else
                {
                    var dto = SalError.CreateDto(SalErrorCodes.Fatal,
                        "При обработке event произошла ошибка"
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
                    "При обработке event произошла ошибка"
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
            var transportMessage = SalSerializer.BinaryDeserialize<Message>(rabbitMessage.Payload.Span);
            if (transportMessage == null)
                throw new Exception($"Неудалось десерилизовать сообщение | CorrelationId:{rabbitMessage.CorrelationId}");
            if (transportMessage.Type != MessageTypes.Event)
                throw new Exception($"Сообщение не Event ({transportMessage.Type}) | CorrelationId:{rabbitMessage.CorrelationId}");

            if (transportMessage.Payload == null)
                throw new Exception($"Отсутствует message.Payload | CorrelationId:{rabbitMessage.CorrelationId}");

            if (transportMessage.Payload.Type == JTokenType.Null)
                throw new Exception($"Отсутствует message.Payload | CorrelationId:{rabbitMessage.CorrelationId}");

            return transportMessage;
        }

        protected EventPayload ExtractEventPayload(Message transportMessage)
        {
            var eventPayload = transportMessage.Payload.ConvertValue<EventPayload>();

            if (eventPayload.Descriptor == null)
                throw new Exception($"Отсутствует eventPayload.Descriptor | CorrelationId:{transportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(eventPayload.Descriptor.EventName))
                throw new Exception($"Пустой eventPayload.Descriptor.EventName | CorrelationId:{transportMessage.CorrelationId}");

            if (eventPayload.Payload == null)
                throw new Exception($"Отсутствует eventPayload.Payload | CorrelationId:{transportMessage.CorrelationId}");

            return eventPayload;
        }

        private async Task Processing(Message message, EventPayload eventPayload)
        {
            var eventLogger = salLogger.GetLogger(eventPayload);

            if (eventPayload.Descriptor.TTL.HasValue &&
                eventPayload.Descriptor.PublishTimeStamp + eventPayload.Descriptor.TTL.Value <= DateTime.UtcNow)
            {
                eventLogger.Trace("Event - протух");
                return;
            }

            if (!string.IsNullOrWhiteSpace(eventPayload.Descriptor.DestinationAdapterType) &&
                !string.Equals(eventPayload.Descriptor.DestinationAdapterType, AdapterConfiguration.AdapterType, StringComparison.InvariantCultureIgnoreCase))
            {
                eventLogger.Trace("Event - не соответсвие DestinationAdapterType");
                return;
            }

            if (!string.IsNullOrWhiteSpace(eventPayload.Descriptor.DestinationAdapterName) &&
                !string.Equals(eventPayload.Descriptor.DestinationAdapterName, AdapterConfiguration.AdapterName, StringComparison.InvariantCultureIgnoreCase))
            {
                eventLogger.Trace("Event - не соответсвие DestinationAdapterName");
                return;
            }




            var eventName = eventPayload.Descriptor.EventName;


            HandlerContext.Type = HandlerTypes.EventHandler;
            HandlerContext.Name = eventName;


            if (eventHandlers.TryGetValue(eventName, out var eventHandlerInfos))
            {
                var handlerTasks = new List<Task>();

                foreach (var eventHandlerInfo in eventHandlerInfos)
                {
                    salLogger.LogHandler(eventPayload, eventHandlerInfo.HandlerType.Name);
                    handlerTasks.Add(ExecuteEventHandlerAsync(eventHandlerInfo, message, eventPayload, eventLogger));
                }

                await Task.WhenAll(handlerTasks.ToArray());
            }
            else
            {
                var dto = SalError.CreateDto(SalErrorCodes.Fatal, "Евент не обрабатываеться", properties: new {eventName});
                throw dto.ToException();
            }
        }

        public Task ExecuteEventHandlerAsync(EventHandlerInfo ehi, Message message, EventPayload eventPayload, ILogger eventLogger)
        {
            using var scope = container.BeginLifetimeScope();


            var executingContext = new ExecutingContext
            {
                Scope = scope,
                SalClient = scope.ResolveNamed<ISalClient>("front"),
                Logger = eventLogger
            };

            var context = new EventContext()
            {
                Descriptor = eventPayload.Descriptor,
                Session = message.Session.DeepClone() as JObject
            };


            var handler = (IEventHandler) scope.Resolve(ehi.HandlerType);

            handler.SetContexts(context, executingContext);

            if (ehi.IsCommon)
                return ExecuteCommonEventHandlerAsync(handler, eventPayload.Payload);
            else
            {
                var evnt = eventPayload.Payload.ConvertValue(ehi.EventType);
                return (Task) ehi.HandlerMethod.Invoke(handler, new[] {evnt});
            }
        }

        private Task ExecuteCommonEventHandlerAsync(object handler, JObject evnt)
        {
            if (handler is ICommonEventHandler eha)
            {
                return eha.Handle(evnt);
            }
            else
            {
                var dto = SalError.CreateDto(SalErrorCodes.Fatal, "Обработчик не являеться общим", properties: new {handlerType = handler.GetType().Name});
                throw dto.ToException();
            }
        }
    }
}