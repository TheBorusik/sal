using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface ICommonEventHandler : IEventHandler
    {
        Task Handle(JObject evnt);
    }
}