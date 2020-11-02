using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SAL.API;
using SAL.API.Command;
using SAL.API.CommandResult;
using SAL.API.Events;
using SAL.API.FrontCommand;
using SAL.API.SystemCommand;
using SAL.Infrastructure;
using SAL.Infrastructure.FrontAttributes;

[assembly: SalAdapterType("SalTest")]
[assembly: SalServiceType("SalTest")]

// ReSharper disable once CheckNamespace
namespace SAL.Test
{
    public class Front : IModule
    {
        public void Configure(ContainerBuilder builder)
        {
            builder.RegisterSalHandler<ResetCacheHandler>();
        }
    }

    
    public class ResetCacheCommand : IHaveResult<Nothing>
    {
        public string ProcessName { get; set; }
        public string ProcessVersion { get; set; }
    }
    
    [SalExternalMethod("Test.J2")]
    public class ResetCacheHandler : BaseFrontCommandHandlerAsync<ResetCacheCommand, Nothing>
    {
        public ResetCacheHandler(ISalClient backClient) : base(backClient)
        {
        }

        public override async Task Handle(ResetCacheCommand command)
        {
            await PublishSuccess(new Nothing());
        }
    }
}