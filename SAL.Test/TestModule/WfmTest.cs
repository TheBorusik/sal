using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace SAL.Test.Wfm
{
    public class WfmTest : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
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
    


  
    
    //dtos
    
    [SalCommandName("WFMTest.Test")]
    public class WFMTestCommand : IHaveResult<WFMTestCommandResult>
    {
        [Required] public string TestString { get; set; }

    }

    public class WFMTestCommandResult : ICommandResult
    {
        public string RetStr { get; set; }

    }

  // extCommand
  
  [SalCommandName("WFM.Start")]
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
