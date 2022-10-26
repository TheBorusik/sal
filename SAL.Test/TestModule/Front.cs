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
            builder.RegisterSalHandler<TestFrontMultiVersionHandler>();
            builder.RegisterSalHandler<TestFrontMultiVersionHandler2>();
            
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

        
        public void Online()
        {
            Task.Run(async () =>  
            {
                var salClient = lifetimeScope.ResolveKeyed<ISalClient>(Contour.Front);
                await salClient.ExecuteCommandWithVersionAsync("Test.TestVer", "1", new { });
                await salClient.ExecuteCommandWithVersionAsync("Test.TestVer", "3", new { });
                var res = await salClient.ExecuteCommandWithVersionAsync("Test.TestVer", "1000", new { });
            });
            
            


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
    
    public class CommandResult2
    {
        public string Version { get; set; }
    }


    [FrontCommandName("Test.TestVer")]
    [SalCommandVersions("1","3")]
    public class TestFrontMultiVersionHandler : BaseFrontCommandHandlerAsync<Nothing, CommandResult2>
    {
        public override async Task Handle(Nothing command)
        {
            await PublishResult(new CommandResult2
            {
                Version = commandContext.Descriptor.Version
            });
        }
    }
    
    [FrontCommandName("Test.TestVer")]
    [SalCommandVersions("1000", "100")]
    public class TestFrontMultiVersionHandler2 : BaseFrontCommandHandlerAsync<Nothing, CommandResult2>
    {
        public override async Task Handle(Nothing command)
        {
            await PublishResult(new CommandResult2
            {
                Version = $"{commandContext.Descriptor.Version}|10000"
            });
        }
    }




    [BackCommandName("SalTester.TestCommand")]
    public class CommandHandler : BaseBackCommandHandlerAsync<Nothing, CommandResult>
    {


        public override async Task Handle(Nothing command)
        {
            logger.Info(HandlerContext.GetData().ToIndentedJson());
            
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