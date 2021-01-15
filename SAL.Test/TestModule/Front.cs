using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Events;
using SAL.API.FrontCommand;
using SAL.Infrastructure;
using SAL.Infrastructure.EventAttributes;
using SAL.Infrastructure.FrontAttributes;


[assembly: SalAdapterType("SalTest")]
[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
            //   builder.RegisterSalHandler<TestEventAdapter>();
       //     builder.RegisterSalHandler<TestExternal>();
            //   builder.RegisterProcessor<TestFront>();
            
            builder.RegisterSalHandler<GetPermissionTreeHandler>();
        }
    }

    public class TestFront : IProcessor
    {
        private ILifetimeScope scope;

        public TestFront(ILifetimeScope scope)
        {
            this.scope = scope;
        }

        public void Start()
        {
        }

        public void Online()
        {
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
        public override  async Task Handle(ExternalHttpRequest request)
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
            await PublishResult(new ExternalHttpResponse
            {
                ContentType = "text/html;charset=UTF-8",
                Body = Encoding.UTF8.GetBytes(returnHtml),
                StatusCode = 200 
            });
        }
    }
    
    
    
    [SalExternalMethod("SalTest.GetPermissionTree")] 
    public class GetPermissionTreeHandler : BaseFrontBackCommandHandlerAsync<GetPermissionTreeCommand , GetPermissionTreeResult>
    {


        public GetPermissionTreeHandler()
        {
        }

        public override async Task Handle(GetPermissionTreeCommand command)
        {
            await PublishResult(new GetPermissionTreeResult
            {
            });
        }
    }
    
    
    [SalServiceType("SalTest")]
    [SalCommandName("Permissions.GetPermissionTree")]
    public class GetPermissionTreeCommand : IHaveResult<GetPermissionTreeResult>
    {
        
    }

    public class GetPermissionTreeResult : ICommandResult
    {
        public PermissionTreeItem[] PermissionTree { get; set; }
    }
    
    public class PermissionTreeItem
    {
        public PermissionTreeItemType Type { get; set; }
        public long? PermissionId { get; set; }
        public long? CatalogId { get; set; }
        public long? ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string StrId { get; set; }
        public JObject PermissionSettings { get; set; }
        public List<PermissionTreeItem> PermissionTree { get; set; }
    }
    
    public enum PermissionTreeItemType
    {
        Unknown,
        Catalog,
        Permission
    }
}