using System;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    public class BaseAuthServerEventConvertor : ICommonEventHandler2
    {
        private ISalClient frontClient;

        public BaseAuthServerEventConvertor(ILifetimeScope scope)
        {
            frontClient = scope.ResolveKeyed<ISalClient>(Contour.Front);
        }

        private ExecutingContext executingContext;
        private EventContext eventContext;
        
        public async Task Handle(JObject evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            this.eventContext = eventContext;
            this.executingContext = executingContext;
            var notifyEvent  = await Convert(eventContext.Descriptor, evnt);
            await frontClient.PublishEventAsync("AuthServer.ClientNotify", notifyEvent, ttl:TimeSpan.FromSeconds(30));

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