using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Infrastructure;

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
            builder.RegisterSalHandler<WFMTestResulHandler>();

            
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
         //   executingContext.Logger.Debug(SessionManager.Current.ToIndentedJson());
         //   var operationId = SessionManager.Current.GetSafeValue(SessionNames.OperationId, 0L);
           // if(operationId < 5)
                await executingContext.SalClient.ExecuteCommandAsync<WFMTestCommand, WFMTestCommandResult>(new WFMTestCommand
                {
                    TestString = "test"
                });
                
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


    public class WFMTestResulHandler : ICommandResultHandlerAsync<WFMTestCommand, WFMTestCommandResult>
    {
        private ExecutingContext executingContext;
        public async Task<bool> ResultHandle(CommandResult<WFMTestCommandResult> result)
        {
    //        executingContext.Logger.Trace(SessionManager.Current.ToIndentedJson());
            
            return true;
        }

        public void SetContexts(CommandResultContext commandContext, ExecutingContext executingContext)
        {
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

  

}
