using System;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Events;
using SAL.API.FrontCommand;
using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;


[assembly: SalAdapterType("SalTest")]
[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
            builder.RegisterSalHandler<TestEventAdapter>();
            builder.RegisterProcessor<TestFront>();

        }
    }

    public class TestFront : IProcessor
    {
        private ILifetimeScope scope;

        public TestFront(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        public void Start()
        {

        }

        public void Online()
        {
            var client = scope.Resolve<ISalClient>();
            client.PublishEventAsync(new TestBackEvent
            {
                Data = "testData"
            });
        }

        public void Offline()
        {

        }

        public void Stop()
        {

        }
    }


    [SalEventName("SRATest")]
    public class TestBackEvent : IEvent
    {
        public string Data { get; set; }
    }
    
    
    [SalEventHandler("SRATest")]
    public class TestEventAdapter : BaseAuthServerEventConvertor
    {
        public TestEventAdapter(ILifetimeScope scope) : base(scope)
        {
        }

        protected override Task<NotifyEvent> Convert(EventDescriptor eventDescriptor, JObject evnt)
        {
            var result =  base.Convert(eventDescriptor, evnt).Result;

            return Task.FromResult(result);
        }
    }
}