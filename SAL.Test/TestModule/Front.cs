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
        public override async Task Handle(ExternalHttpRequest request)
        {
            await PublishResult(new ExternalHttpResponse
            {
                Body = new byte[0],
                StatusCode = 200,
            });
        }
    }
    
}