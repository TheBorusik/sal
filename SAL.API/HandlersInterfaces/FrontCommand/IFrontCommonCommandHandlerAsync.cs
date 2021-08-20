using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    [Obsolete("Use IFrontCommonCommandHandler2Async")]
    public interface IFrontCommonCommandHandlerAsync : IFrontCommandHandler
    {
        Task Handle(JObject command);
    }
}