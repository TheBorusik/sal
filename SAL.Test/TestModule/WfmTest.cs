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
            
            backClient.PublishCommandAsync(new StartProcessCommand
            {
                ProcessName = "WFM\\Tests\\SimpleTest",
                ResultAdapterType = AdapterConfiguration.AdapterType,
                ProcessCorrelationId = Guid.NewGuid().ToString("N"),
                InitialData = new
                {
                    Str = "test"
                }
                
            });
            
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


    public class WFMTestCommandHandler : BaseCommandHandlerAsync<WFMTestCommand, WFMTestCommandResult>
    {
        
        public override async Task Handle(WFMTestCommand command)
        {
            await PublishResult(new WFMTestCommandResult {
                RetStr = command.TestString.ToUpper()
            });
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

  // extCommand
  
  [SalServiceType("WFM")]
  [SalCommandName("Start")]
  public class StartProcessCommand : IHaveResult<StartProcessCommandResult>
  {
      [Required]
      public string ProcessName { get; set; }
      public string Version { get; set; }
      [Required] public object InitialData { get; set; }

      public string ResultAdapterType { get; set; }
      public string ResultAdapterName { get; set; }
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

}
