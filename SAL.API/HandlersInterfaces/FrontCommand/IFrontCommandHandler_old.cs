using System.Threading.Tasks;
using SAL.Infrastructure;

/*
namespace SAL.API.Command
{
    
    public interface IFrontCommandHandler
    {

    }

    public interface IFrontCommandHandler<TCommand, TCommandResult> : IFrontCommandHandler
        where TCommand : class, IHaveResult<TCommandResult>, new()
        where TCommandResult : class, ICommandResult, new()
    {
        Task<TCommand> Deserialize(byte[] data);
        Task Validate(TCommand command);
        Task<TCommandResult> Handle(TCommand command);
        Task<byte[]> Serialize(TCommandResult result);
    }
}
*/