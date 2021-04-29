using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.Core.S3;
using SAL.Infrastructure;

[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder, IConfigWatcher config)
        {
            //builder.RegisterSalHandler<TestEventAdapter>();
            builder.RegisterSalHandler<TestExternal>();
            builder.RegisterSalHandler<GetPermissionTreeHandler>(); 
            builder.RegisterProcessor<TestFront>();

            //  builder.RegisterSalHandler<GetPermissionTreeHandler>();
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
         /*
            using var scope = lifetimeScope.BeginLifetimeScope();
            var s3Store = scope.Resolve<IS3Store>();
            try
            {
                s3Store.UploadFileAsync(@"G:\Downloads\aida64extreme632.zip", "tmp", "aida64extreme632.zip").Wait();
            //    s3Store.DownloadFileAsync("tmp", "aida64extreme632.zip",@"G:\aida64.zip").Wait();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            */
         
         using var scope = lifetimeScope.BeginLifetimeScope();

         var cl = scope.ResolveNamed<ISalClient>("front");

         var sesPre = SessionManager.Current.Clone();
         
         var res = cl.ExecuteCommandAsync<GetPermissionTreeCommand, GetPermissionTreeResult>(new GetPermissionTreeCommand { }).Result;

         var sesPost = SessionManager.Current.Clone();
         
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

            SessionManager.Current.AddOrUpdate("Test", "Test");
            await PublishResult(new ExternalHttpResponse
            {
                ContentType = "text/html;charset=UTF-8",
                Body = Encoding.UTF8.GetBytes(returnHtml),
                StatusCode = 200
            });
        }
    }


    [SalExternalMethod("SalTest.TestCommand")]
    public class GetPermissionTreeHandler : BaseFrontBackCommandHandlerAsync<GetPermissionTreeCommand, GetPermissionTreeResult>
    {
        private static int index = 0;
        public GetPermissionTreeHandler()
        {
        }

        public override async Task Handle(GetPermissionTreeCommand command)
        {
            SessionManager.Current.AddOrUpdate("Test1", $"Value_{index++}");
            await PublishResult(new GetPermissionTreeResult
            {
                Session = SessionManager.Current
            });
            SessionManager.Current.AddOrUpdate("Test2", $"Value_{index++}");
        }
    }


    [SalServiceType("SalTest")]
    [SalCommandName("TestCommand")]
    public class GetPermissionTreeCommand : IHaveResult<GetPermissionTreeResult>
    {
    }

    public class GetPermissionTreeResult : ICommandResult
    {
        public JObject Session { get; set; } 
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