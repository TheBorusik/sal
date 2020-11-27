using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.API.Command;
using SAL.API.SystemCommand;
using SAL.Infrastructure;
using SAL.Infrastructure.ValidationAttribute;

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class WfmTest : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {

            builder.RegisterSalHandler<WfmResultHandler1>();
            builder.RegisterSalHandler<WfmResultHandler2>();
            
            builder.RegisterSalHandler<WFMTestCommandHandler>();
            builder.RegisterProcessor<WfmTestProcessor>();

        }
    }

    public class WfmTestProcessor : IProcessor
    {
        private readonly ILogger<TestProcessor> logger;
        private readonly ILifetimeScope scope;


        public WfmTestProcessor(ILogger<TestProcessor> logger, ILifetimeScope scope)
        {
            this.logger = logger;
            this.scope = scope;
            // this.client = scope.ResolveNamed<ISalClient>("front");
            // this.client = scope.Resolve<ISalClient>();
        }

        public void Start()
        {

            logger.LogInformation("Тестовое сообщение", new { MercId = 10 });


        }

        public void Online()
        {

            var backClient = scope.Resolve<ISalClient>();


            /*
            backClient.PublishCommandAsync(new StartProcessCommand
            {
                ProcessName = "Дебит WoF Finish",
                ResultHandlerType = AdapterConfiguration.AdapterType,
                ProcessCorrelationId = Guid.NewGuid().ToString("N"),
                InitialData = new
                {
                    Order = new 
                    {
                        OrderId = 100010,

                    },
                    Terminal = new
                    { 
                        MercId = 1,
                        Mps = "VISA",
                        Channel = "1",
                        Is3Ds = true,
                        TerminalId = "12",
                        MerchantId = "123123",
                        Mcc = "123",
                        Name = "test1",
                        MerchantUrl = "https://yandex.ru",
                        Currency = "RUB",
                        Gate = "TCB",
                        OperType = "Test"

                    },
                    AcsInfo = new 
                    {
                        PaRes = "dsaasdasd",
                        Md = "1234"
                    }
                }
                
            });
            */


        }

        public void Offline()
        {

        }

        public void Stop()
        {

        }
    }

    [WfmResultHandlerName("test")]

    public class WfmResultHandler1 : IWfmResultHandler
    {
        public Task Handle(CommonCommandResult processResult, WfmProcessInfo ProcessInfo)
        {
            return Task.CompletedTask;
        }
    }
    


    public class WfmResultHandler2 : IWfmResultHandler
    {
        public Task Handle(CommonCommandResult processResult, WfmProcessInfo ProcessInfo)
        {
            return Task.CompletedTask;
        }
    }


    public class WFMTestCommandHandler : ICommandHandlerAsync<WFMTestCommand, WFMTestCommandResult>
    {
        private CommandContext commandContext;
        private ExecutingContext executingContext;


        public async Task Handle(WFMTestCommand command)
        {
            await executingContext.SalClient.PublishResultAsync(new WFMTestCommandResult
            {
                RetStr = command.TestString.ToUpper()
            }, commandContext.Descriptor);
        }

        public void SetContexts(CommandContext commandContext, ExecutingContext executingContext)
        {
            this.commandContext = commandContext;
            this.executingContext = executingContext;
        }
    }

    //dtos

    [SalServiceType("WFMTest")]
    [SalCommandName("Test")]
    public class WFMTestCommand : IHaveResult<WFMTestCommandResult>
    {
        [Required] public string TestString { get; set; }

    }

    public class WFMTestCommandResult : ICommandResult
    {
        public string RetStr { get; set; }

    }

    [SalServiceType("WFM")]
    [SalCommandName("Start")]
    public class StartProcessCommand : IHaveResult<StartProcessCommandResult>
    {
        [Required]
        public string ProcessName { get; set; }
        public string Version { get; set; }
        [Required]
        public object InitialData { get; set; }

        [Required]
        public string ResultHandlerType { get; set; }
        public string ResultHandlerName { get; set; }
        public string ProcessCorrelationId { get; set; }


        public int? Priority { get; set; }
    }

    public class StartProcessCommandResult : ICommandResult
    {
        public long ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string Version { get; set; }
        public int Priority { get; set; }

    }

    [SalServiceType("WFM")]
    [SalCommandName("Result")]
    public class ProcessResultCommandResult : IHaveResult<Nothing>
    {
        public string ProcessCorrelationId { get; set; }
        public long ProcessId { get; set; }
        public string ProcessName { get; set; }
        public string Version { get; set; }

        public CommonCommandResult ProcessResult { get; set; }
    }

}
