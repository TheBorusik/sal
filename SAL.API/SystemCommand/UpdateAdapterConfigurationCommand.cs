using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    [SalServiceType("System")]
    [SalCommandName("UpdateAdapterConfiguration")]
    public class UpdateAdapterConfigurationCommand : IHaveResult<Nothing>
    {
        public string AdapterType { get; set; }
        public string AdapterName { get; set; }
        public JObject Configuration { get; set; }
    }
}