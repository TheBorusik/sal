using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API.Command
{
    public interface ICommonCommandHandlerAsync
    {
        Task Handle(JObject command, CommandContext context, ExecutingContext executingContext);
    }
}