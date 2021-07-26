using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
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

            builder.RegisterSalHandler<TestExternal>();
            builder.RegisterSalHandler<FrontTestCommandHandler>();
            builder.RegisterSalHandler<BackTestCommandHandler>();
            
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
         
       //  back.PublishFrontCommandAsync("SalTest.FrontTest",new TestCommand()).Wait();
         back.PublishCommandAsync(new TestCommand()).Wait();
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



    [SalCommandName("SalTest.Front.Test")]
    [SalRequestType("SalTest.FrontTest")]
    public class TestCommand : IHaveResult<TestResult>
    {
        public int Int { get; set; }
        public string Str { get; set; }
    }

    public class TestResult : ICommandResult
    {
        public int Int { get; set; }
        public string Str { get; set; }
    }

    [SalCommandName("SalTest.Front.Test")]
    public class Test2Result : ICommandResult
    {
        public int Int { get; set; }
        public string Str { get; set; }
    }
    
   
    
    public class  FrontTestCommandHandler : BaseFrontCommandHandlerAsync<TestCommand, TestResult>
    {
        public FrontTestCommandHandler(ISalClient backClient) : base(backClient)
        {
        }

        public override async Task Handle(TestCommand command)
        {
            await PublishResult(new TestResult
            {
                Int = 10,
                Str = "100"
            });
        }
    }


    public class BackTestCommandHandler : BaseCommandHandlerAsync<TestCommand, TestResult>
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
    
    
    [SalContourHandler(Contour.Back)]
    public class TestCommandResultHandler3 : ICommandResultHandle2Async<Test2Result>
    {
        public Task<bool> ResultHandle(CommandResult<Test2Result> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
    }
    
    
    [SalContourHandler(Contour.Back)]
    public class TestCommandResultHandler1 : ICommandResultHandle2Async<TestCommand, TestResult>
    {
        public Task<bool> ResultHandle(CommandResult<TestResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
    }
    
    [SalContourHandler(Contour.Front)]
    public class TestCommandResultHandler2 : ICommandResultHandle2Async<TestCommand, TestResult>
    {
        public Task<bool> ResultHandle(CommandResult<TestResult> result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
    }

    [SalContourHandler(Contour.Back)]
    [SalRequestType("SalTest.FrontTest")]
    public class TestCommonCommandResultHandler : ICommonCommandResultHandler2
    {
        public Task<bool> ResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext)
        {
            return Task.FromResult(true);
        }
    }
}