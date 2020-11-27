using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API.FrontCommand
{
    public interface IFrontCommonCommandHandlerAsync : IFrontCommandHandler
    {
        Task Handle(JObject command);
    }
}