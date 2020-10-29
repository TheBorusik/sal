using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API.SystemCommand
{
    [SalServiceType("System")]
    [SalCommandName("GetAdapterConfiguration")]
    public class GetAdapterConfigurationCommand : IHaveResult<GetAdapterConfigurationResult>
    {
    }
    public class GetAdapterConfigurationResult : ICommandResult
    {
        public string AdapterType { get; set; }
        public string AdapterName { get; set; }
        public JObject Configuration { get; set; }
    }
}