using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API.Command
{
    public interface ICommonCommandHandler : ICommandHandler
    {
        Task Handle(JObject command);
    }
}