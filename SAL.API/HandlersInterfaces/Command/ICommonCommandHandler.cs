using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface ICommonCommandHandler : ICommandHandler
    {
        Task Handle(JObject command);
    }
}