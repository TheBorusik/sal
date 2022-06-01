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

            
       //     builder.RegisterSalHandler<TestEventHandler>();
        //    builder.RegisterSalHandler<ExceptionDetectedEventHandler>();
            
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
            
            client.PublishCommandWithSharedResultHandlerAsync("SalTester.TestCommand", new { });
            client.PublishCommandWithSharedResultHandlerAsync("SalTester.TestCommand", new { });

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


    [BackCommandName("SalTester.TestCommand")]
    public class CommandHandler : BaseBackCommandHandlerAsync<Nothing, CommandResult>
    {


        public override async Task Handle(Nothing command)
        {
            await PublishResult(new CommandResult
            {
                Id = 1,
                Payload = "s1"
            });
        }
    }
    
    
    public class CommandResultHandler : ICommonCommandSharedResultHandler
    {
        private ILogger logger;

        public CommandResultHandler(ILoggerProvider loggerProvider)
        {
            this.logger = loggerProvider.CreateLogger("CommonCommandSharedResultHandler");
        }

        public Task PersonalResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            logger.Info("!!! PERSONAL !!!");
            return Task.CompletedTask;
        }

        public Task SharedResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            logger.Info("!!!! SHARED !!!!");
            return Task.CompletedTask;
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