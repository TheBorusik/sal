using System.Threading.Tasks;

namespace SAL.API.FrontCommand
{
    public interface IFrontExternalHttpMethod
    {
        Task Handle(ExternalHttpRequest payload, CommandContext context, ExecutingContext executingContext);
    }
}