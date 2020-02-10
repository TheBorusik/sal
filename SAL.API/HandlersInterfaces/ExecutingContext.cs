using Autofac;
using Microsoft.Extensions.Logging;
using SAL.API.Client;

namespace SAL.API
{
    public class ExecutingContext
    {
        public ISalClient SalClient { get; set; }
        public  ILifetimeScope Scope { get; set; }
        public ILogger Logger { get; set; }
    }
}