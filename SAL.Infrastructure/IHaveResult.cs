namespace SAL.Infrastructure
{

    public interface IHaveResult<TCommandResult> : ICommand
        where TCommandResult : class, ICommandResult, new()
    {

    }
}