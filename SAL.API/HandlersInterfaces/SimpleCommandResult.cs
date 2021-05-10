namespace SAL.API
{
    public class SimpleCommandResult
    {
        public CommonCommandResult CommandResult;
        public CommandResultContext CommandResultContext;

        public SimpleCommandResult Clone()
        {
            return new()
            {
                CommandResult = CommandResult.Clone(),
                CommandResultContext = CommandResultContext.Clone()
            };
        }
    }
}