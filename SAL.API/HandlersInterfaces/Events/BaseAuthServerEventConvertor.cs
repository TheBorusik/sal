using System;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    
    public class NotifyEvent : IEvent
    {
        public string EventName { get; set; }
        public JObject Payload { get; set; }
        public DateTime? TimeStamp { get; set; }
    }
    
    public class BaseAuthServerEventConvertor : ICommonEventHandler
    {
        private ILoSalClient frontClient;

        public BaseAuthServerEventConvertor(ILifetimeScope scope)
        {
            frontClient = scope.ResolveNamed<ILoSalClient>("front");
        }

        private ExecutingContext executingContext;
        private EventContext eventContext;

        public void SetContexts(EventContext eventContext, ExecutingContext executingContext)
        {
            this.eventContext = eventContext;
            this.executingContext = executingContext;
        }

        public async Task Handle(JObject evnt)
        {
            var notifyEvent  = await Convert(eventContext.Descriptor, evnt);
            await frontClient.PublishEventAsync("AuthServer.ClientNotify", notifyEvent, eventContext.Descriptor.CorrelationId, TimeSpan.FromSeconds(30), false, null, null);
        }

        protected virtual async Task<NotifyEvent> Convert(EventDescriptor eventDescriptor, JObject evnt)
        {
            return new NotifyEvent
            {
                 EventName = await ConvertEventName(eventDescriptor),
                 Payload = await ConvertEventBody(evnt),
                 TimeStamp = eventDescriptor.PublishTimeStamp
            };
        }

        protected virtual  Task<JObject> ConvertEventBody(JObject body)
        {
            return Task.FromResult(body.Clone());
        }

        protected virtual Task<string> ConvertEventName(EventDescriptor eventDescriptor)
        {
            return Task.FromResult(eventDescriptor.EventName);
        }
    }
}