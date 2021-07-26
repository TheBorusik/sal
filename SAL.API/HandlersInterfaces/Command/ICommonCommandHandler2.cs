using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface ICommonCommandHandler2 : ICommandHandler2
    {
        Task Handle(JObject command,CommandContext commandContext, ExecutingContext executingContext);
    }
}