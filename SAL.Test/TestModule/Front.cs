using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.Rabbit.Consts;
using SAL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace SAL.Test.Front
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
            builder.RegisterSalHandler<CommandHandler>();
            builder.RegisterSalHandler<CommandResultHandler>();
            builder.RegisterSalHandler<ExceptionDetectedEventHandler>();
            
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

            client.PublishCommandAsync("SalTester.TestCommand", new { });
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

        [SalEventName("System.ExceptionDetected")]
        public async Task Handle(ExceptionDetectedEvent evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            logger.Info(evnt.ToIndentedJson());
        }

    }


}