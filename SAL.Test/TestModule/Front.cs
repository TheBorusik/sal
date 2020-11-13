using System;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using SAL.API;
using SAL.API.FrontCommand;
using SAL.Infrastructure;


[assembly: SalAdapterType("SalTest")]
[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder)
        {
            builder.RegisterSalHandler<TestEH>();
            
            builder.RegisterSalHandler<TestCommandHandler>();
            builder.RegisterSalHandler<CommonCommandResultHandler>();
        }
    }



    
    [SalExternalHttpPath("/api/test")]
    public class TestEH : FrontExternalHttpMethod
    {
        static int index = 0;
        public override async Task Handle(ExternalHttpRequest request)
        {
            await PublishResult(new ExternalHttpResponse
            {
                Body = Encoding.UTF8.GetBytes(new {test = 110, tests = ++index , dt = DateTime.UtcNow}.ToIndentedJson()),
                StatusCode = 200,
            });
        }
    }
    
}