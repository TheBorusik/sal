using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface ICommonEventHandler2 : IEventHandler2
    {
        Task Handle(JObject evnt, EventContext eventContext, ExecutingContext executingContext);
    }
}