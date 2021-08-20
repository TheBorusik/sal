using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    [Obsolete("Use ICommonEventHandler2")]
    public interface ICommonEventHandler : IEventHandler
    {
        Task Handle(JObject evnt);
    }
}