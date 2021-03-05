using System.Threading.Tasks;

namespace SAL.API
{
    public interface IFrontExternalHttpMethod
    {
        Task Handle(ExternalHttpRequest payload, CommandContext context, ExecutingContext executingContext);
    }
}