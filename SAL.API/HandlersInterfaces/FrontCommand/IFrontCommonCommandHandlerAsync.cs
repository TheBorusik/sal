using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    [Obsolete]
    public interface IFrontCommonCommandHandlerAsync : IFrontCommandHandler
    {
        Task Handle(JObject command);
    }
}