using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API.Events
{
    public interface ICommonEventHandler : IEventHandler
    {
        Task Handle(JObject evnt);
    }
}