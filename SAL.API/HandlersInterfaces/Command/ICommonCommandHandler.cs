using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    [Obsolete("Use ICommonCommandHandler2")]
    public interface ICommonCommandHandler : ICommandHandler
    {
        Task Handle(JObject command);
    }
}