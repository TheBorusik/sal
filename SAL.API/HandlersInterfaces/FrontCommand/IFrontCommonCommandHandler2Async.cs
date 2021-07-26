using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace SAL.API
{
    public interface IFrontCommonCommandHandler2Async : IFrontCommandHandler2
    {
        Task Handle(JObject command, CommandContext commandContext, ExecutingContext executingContext);
    }
}