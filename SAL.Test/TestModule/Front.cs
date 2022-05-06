using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Infrastructure;


// ReSharper disable once CheckNamespace
namespace SAL.Test.Front
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
            builder.RegisterSalHandler<CommandResultHandler>();
            builder.RegisterSalHandler<CommandHandler>();

            
            builder.RegisterSalHandler<TestEventHandler>();
            builder.RegisterSalHandler<ExceptionDetectedEventHandler>();
            
            //     builder.RegisterSalHandler<ExternalApi>();
            builder.RegisterProcessor<TestFront>();
        }
    }

    public class TestFront : IProcessor
    {
        private ILifetimeScope lifetimeScope;

        public TestFront(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }


        public void Start()
        {
        }


        static int __index = 0;

        public void Online()
        {
            using var scope = lifetimeScope.BeginLifetimeScope();

            
            

            var client = scope.Resolve<ISalClient>();



            for (var i = 1; i < 100; i += 5)
            {
                try
                {

                    var dataSize = i * 1024 * 1024;
                    Console.WriteLine($"Оправляем  dataSize {dataSize.HumanReadable()}");
                    client.PublishEventAsync("TestEvent1", new { 
                        Size = i, 
                        Data = new string('*', dataSize) }).Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            //    client.PublishCommandAsync("SalTester.TestCommand", new { });


        }

        public void Offline()
        {
        }

        public void Stop()
        {
        }
    }


    public class CommandResult
    {
        public int Id { get; set; }
        public string Payload { get; set; }
    }


    [SalContourHandler(Contour.Back)]
    [SalCommandName("SalTester.TestCommand")]
    public class CommandHandler : ICommonCommandHandler2
    {
        public Task Handle(JObject command, CommandContext commandContext, ExecutingContext executingContext)
        {
            throw new Exception("Test");
        }
    }


    [SalContourHandler(Contour.Back)]
    public class CommandResultHandler : ICommandResultHandle2Async<CommandResult>
    {
        [SalCommandName("SalTester.TestCommand")]
        public async Task<bool> ResultHandle(CommandResult<CommandResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return true;
        }
    }


    [SalContourHandler(Contour.Both)]
    public class ExceptionDetectedEventHandler :
        IEventHandler2<ExceptionDetectedEvent>
    {
        private ILogger logger;

        public ExceptionDetectedEventHandler(ILoggerProvider loggerProvider)
        {
            this.logger = loggerProvider.CreateLogger("Test");
        }

        [SalEventName("System.ExceptionDetected", false)]
        public async Task Handle(ExceptionDetectedEvent evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            logger.Info(evnt.ToIndentedJson());
        }

    }


    public class TestEvent1
    {
        public int Size { get; set; }
        public JToken Data { get; set; }
        
    }

    public class TestEvent2
    {
        
    }
    

    [SalContourHandler(Contour.Back)]
    public class TestEventHandler :
        IEventHandler2<TestEvent1>
    {
        private ILogger logger;

        public TestEventHandler(ILoggerProvider loggerProvider)
        {
            this.logger = loggerProvider.CreateLogger("Test");
        }

        [SalEventName("TestEvent1", false,  true)]
        public async Task Handle(TestEvent1 evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            var str = evnt.ToIndentedJson();
            if(str.Length < 1024)
                logger.Info(str);
            else
                logger.Info(str.Substring(0,256) + $"[256 of {str.Length.HumanReadable()}]");
        }
        
    }


/*
    [SalExternalHttpPathAttribute("/api/saltest/test", "/api/saltest/test/[a-z0-9]+$")]
    public class ExternalApi : FrontExternalHttpMethod
    {
        public override async Task Handle(ExternalHttpRequest request)
        {
            await PublishResult(new ExternalHttpResponse
            {
                StatusCode = 200
            });
        }


    }
    */
}