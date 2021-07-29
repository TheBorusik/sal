using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

            builder.RegisterSalHandler<TestExternal>();
            builder.RegisterSalHandler<FrontTestCommandHandler>();
     //       builder.RegisterSalHandler<BackTestCommandHandler>();
            
            builder.RegisterSalHandler<TestCommandResultHandler1>();
            builder.RegisterSalHandler<TestCommandResultHandler3>();

            
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

         front.PublishCommandAsync("SalTest.FrontTest", new TestCommand()).Wait();

         back.PublishCommandAsync("SalTest.Front.Test",new TestCommand()).Wait();
        }

        public void Offline()
        {
        }

        public void Stop()
        {
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

    
    public class TestCommand 
    {
        
        public int[] Int { get; set; }
        public string Str { get; set; }
    }
    public class TestResult 
    {
        public int Int { get; set; }
        public string Str { get; set; }
    }
    public class Test2Result 
    {
        public int Int2 { get; set; }
        public string Str2 { get; set; }
    }


    

    [BackCommandName("SalTest.Front.Test")]
    [FrontCommandName("SalTest.FrontTest")]
    public class  FrontTestCommandHandler : BaseFrontBackCommandHandlerAsync<TestCommand, TestResult>
    {

        public override async Task Handle(TestCommand command)
        {
            await PublishResult(new TestResult
            {
                Int = 10,
                Str = "100"
            });
        }
    }



    
    
    [SalContourHandler(Contour.Both)]
    [SalCommandName("SalTest.Front.Test")]
    public class TestCommandResultHandler3 : ICommandResultHandle2Async<Test2Result>, ICommandResultHandle2Async<TestResult>
    {
        [SalCommandName("SalTest.Front.Test")]
        public Task<bool> ResultHandle(CommandResult<Test2Result> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
        
        [SalCommandName("SalTest.Front.Test")]
        public Task<bool> ResultHandle(CommandResult<TestResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            throw new System.NotImplementedException();
        }
    }
    
    
    [SalContourHandler(Contour.Front)]
    [SalCommandName("SalTest.FrontTest")]
    public class TestCommandResultHandler1 : ICommandResultHandle2Async<TestResult>
    {
        public Task<bool> ResultHandle(CommandResult<TestResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
    }
    
   

    [SalContourHandler(Contour.Back)]

    public class TestCommonCommandResultHandler : ICommonCommandResultHandler2
    {
        public Task<bool> ResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
    }
}