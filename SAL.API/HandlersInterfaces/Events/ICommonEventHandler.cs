using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API.Events
{
    public interface ICommonEventHandler
    {
        Task Handle(JObject evnt, EventDescriptor context, ExecutingContext executingContext);
    }
}