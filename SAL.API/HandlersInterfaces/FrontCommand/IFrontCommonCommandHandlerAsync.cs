using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface IFrontCommonCommandHandlerAsync : IFrontCommandHandler
    {
        Task Handle(JObject command);
    }
}