using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using SAL.API;
using SAL.Infrastructure;

// ReSharper disable once CheckNamespace
namespace SAL.Test.Front
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {

         //   builder.RegisterSalHandler<TestExternal>();
         //   builder.RegisterSalHandler<FrontTestCommandHandler>();
         //   builder.RegisterSalHandler<GateEventHandler>();
     //       builder.RegisterSalHandler<BackTestCommandHandler>();
     
     
            
           // builder.RegisterSalHandler<TestCommandResultHandler1>();
        //    builder.RegisterSalHandler<TestCommandResultHandler3>();
            builder.RegisterSalHandler<SendCommandResultHandler>();

            
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

         
         using var scope = lifetimeScope.BeginLifetimeScope();

         var front = scope.ResolveKeyed<ISalClient>(Contour.Front);
         var back = scope.ResolveKeyed<ISalClient>(Contour.Back);

         back.PublishCommandAsync("Observer.SendCommandResult1", new { });

        }

        public void Offline()
        {
        }

        public void Stop()
        {
        }
    }
    
    internal class GateEventHandler : 
        IEventHandler2<IAmOffline>, 
        IEventHandler2<HeartbeatEvent>
    {
        public Task Handle(IAmOffline evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            return Task.CompletedTask;
        }
        
        public Task Handle(HeartbeatEvent evnt, EventContext eventContext, ExecutingContext executingContext)
        {
            return Task.CompletedTask;
        }
    }

    
    public class SendCommandResultCommand : IHaveResult<Nothing>
    {
        public string CorrelationId { get; set; }
        public CommonCommandResult CommandResult { get; set; }       
    }
    
    
    
    [FrontCommandName("Observer.SendCommandResult1")]
    [BackCommandName("Observer.SendCommandResultCom1")]
    public class SendCommandResultHandler : BaseFrontBackCommandHandlerAsync<SendCommandResultCommand, Nothing>
    {
        public override  Task Handle(SendCommandResultCommand command)
        {
            return Task.CompletedTask;
        }
    }
    
    
    
    [SalExternalHttpPath("/api/ehtest")]
    public class TestExternal : FrontExternalHttpMethod
    {
        public override async Task Handle(ExternalHttpRequest request)
        {
            var t = request.FormData.GetSafeValue("TermUrl", "");
            var returnHtml = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
</head>
<body>
<form name=""postform"" action=""{t}"" method=""POST"">
<input type=""hidden"" name=""MD"" value=""My_Md"">
<input type=""hidden"" name=""PaRes"" value=""My_PaRes"">
<center>Please click Submit to continue.<br>
<input type=""submit"" name=""submit"" value=""Submit""/></center>
</form>
</body>
</html>
";

            //     SessionManager.Current.AddOrUpdate("Test", "Test");
            await PublishResult(new ExternalHttpResponse
            {
                ContentType = "text/html;charset=UTF-8",
                Body = Encoding.UTF8.GetBytes(returnHtml),
                StatusCode = 200
            });
        }
    }
}