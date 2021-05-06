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
using SAL.Core.Helpers;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Service;
using SAL.Infrastructure;


namespace SAL.Core.Processors
{
    internal class EventProcessor : IProcessor
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
        private readonly List<EventHandlerInfo> anyEventHandlers = new List<EventHandlerInfo>();

        public EventProcessor(ILifetimeScope container, ILoggerProvider loggerProvider, ISalLogger salLogger)
        {
            this.container = container;
            this.loggerProvider = loggerProvider;
            this.salLogger = salLogger;
            logger = loggerProvider.CreateLogger(nameof(EventProcessor));
            salService = container.Resolve<ISalService>();
        }
        
        public void Start()
        {
            try
            {
                salClient = container.Resolve<ISalClient>();

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(IEventHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterEventHandler);

                container.ComponentRegistry.Registrations
                    .Where(r => r.Services.OfType<TypedService>().Any(ts => ts.ServiceType == typeof(ICommonEventHandler)))
                    .Select(a => a.Activator.LimitType)
                    .ForEach(RegisterCommonEventHandler);

                var configWatcher = container.Resolve<IConfigWatcher>();
                var jsonConfig = configWatcher.GetSection(ConfigurationSectionNames.EventProcessor);
                var config = new EventProcessorConfig();

                if (jsonConfig != null)
                {
                    config = jsonConfig.ConvertValue<EventProcessorConfig>();
                }


                var transport = container.Resolve<ITransport>();

                var subscriptionFactory = transport.CreateMessageSubscription();

                var eventList = eventHandlers.Where(eh => eh.Value.Any(h => h.IsSystem == false)).Select(eh => eh.Key).ToArray();
                var systemEventList = eventHandlers.Where(eh => eh.Value.Any(h => h.IsSystem == true)).Select(eh => eh.Key).ToArray();

                subscription = subscriptionFactory.CreateEvent(config.PrefetchCount, eventList, Handler);
                systemSubscription = subscriptionFactory.CreateSystemEvent(config.SystemPrefetchCount, systemEventList, Handler);
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
                    salService.AddBackEventHandler(new API.EventHandlerInfo
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
            


            if (attrs.Any())
            {
                IEventDtoCreator dtoCreater = null;
                if (handlerType.IsAssignableTo<IEventDtoCreator>())
                    dtoCreater = (IEventDtoCreator)container.Resolve(handlerType);
                
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
                        
                        var eventInfo = new API.EventHandlerInfo
                        {
                            IsSystem = eventHandlerInfo.IsSystem,
                            IsCommon = eventHandlerInfo.IsCommon,
                            EventName = eventName,
                            Dtos = new DtoInfo[0]
                        };

                        if (dtoCreater != null)
                        {
                            eventInfo.EventDto = dtoCreater.GetEventDtoName(eventName);
                            eventInfo.Dtos = dtoCreater.GetEventDtos(eventName);
                        }
                        
                        
                        salService.AddBackEventHandler(eventInfo);
                    }

                    logger.Info($"Для евента {eventName} добавлен уневерсальный обработчик результата {handlerType.Name}");
                });
            }
            else
            {
                var eventHandlerInfo = new EventHandlerInfo
                {
                    HandlerType = handlerType,
                    EventName = null,
                    EventType = null,
                    IsSystem = false,
                    HandlerMethod = null,
                    IsCommon = true,
                };

                anyEventHandlers.Add(eventHandlerInfo);
                logger.Info($"Добавлен уневерсальный обработчик событий {handlerType.Name}");
            }
        }

        private async Task Handler(RabbitMessage rabbitMessage, Action ack, Action nack)
        {
            try
            {
                HandlerContext.Set(HandlerTypes.Processor, "EventProcessor", rabbitMessage.CorrelationId);
                var transportMessage = ExtractMessage(rabbitMessage);
                var eventPayload = ExtractEventPayload(transportMessage);
                HandlerContext.Update(transportMessage);
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
                sb.AppendLine(SalEncoding.GetString(rabbitMessage.Payload));
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

        protected TransportMessage ExtractMessage(RabbitMessage rabbitMessage)
        {
            var transportMessage = SalSerializer.BinaryDeserialize<TransportMessage>(rabbitMessage.Payload);
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

        protected EventPayload ExtractEventPayload(TransportMessage transportTransportMessage)
        {
            var eventPayload = transportTransportMessage.Payload.ConvertValue<EventPayload>();

            if (eventPayload.Descriptor == null)
                throw new Exception($"Отсутствует eventPayload.Descriptor | CorrelationId:{transportTransportMessage.CorrelationId}");

            if (string.IsNullOrWhiteSpace(eventPayload.Descriptor.EventName))
                throw new Exception($"Пустой eventPayload.Descriptor.EventName | CorrelationId:{transportTransportMessage.CorrelationId}");

            if (eventPayload.Payload == null)
                throw new Exception($"Отсутствует eventPayload.Payload | CorrelationId:{transportTransportMessage.CorrelationId}");

            return eventPayload;
        }

        private async Task Processing(TransportMessage transportMessage, EventPayload eventPayload)
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

            HandlerContext.Update(HandlerTypes.EventHandler, eventName);
            
            var handlerTasks = new List<Task>();

            foreach (var eventHandlerInfo in anyEventHandlers)
            {
                salLogger.LogHandler(eventPayload, eventHandlerInfo.HandlerType.Name);
                handlerTasks.Add(ExecuteEventHandlerAsync(eventHandlerInfo, transportMessage, eventPayload, eventLogger));
            }

            if (eventHandlers.TryGetValue(eventName, out var eventHandlerInfos))
            {
                foreach (var eventHandlerInfo in eventHandlerInfos)
                {
                    salLogger.LogHandler(eventPayload, eventHandlerInfo.HandlerType.Name);
                    handlerTasks.Add(ExecuteEventHandlerAsync(eventHandlerInfo, transportMessage, eventPayload, eventLogger));
                }
            }

            if (handlerTasks.Any())
            {
                await Task.WhenAll(handlerTasks.ToArray());
            }
            else
            {
                var dto = SalError.CreateDto(SalErrorCodes.Fatal, "Евент не обрабатываеться", properties: new {eventName});
                throw dto.ToException();
            }
        }

        public Task ExecuteEventHandlerAsync(EventHandlerInfo ehi, TransportMessage transportMessage, EventPayload eventPayload, ILogger eventLogger)
        {
            using var scope = container.BeginLifetimeScope();


            var executingContext = new ExecutingContext
            {
                Scope = scope,
                SalClient = scope.Resolve<ISalClient>(),
                Logger = eventLogger
            };

            var context = new EventContext()
            {
                Descriptor = eventPayload.Descriptor,
                SessionId = HandlerContext.SessionId,
                AuthId = HandlerContext.AuthId,
                ProcessId = HandlerContext.ProcessId
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