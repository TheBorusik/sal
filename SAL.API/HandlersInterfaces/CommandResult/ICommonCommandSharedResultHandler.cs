using System.Threading.Tasks;

namespace SAL.API
{
    public interface ICommonCommandSharedResultHandler
    {
        Task PersonalResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext);
        Task SharedResultHandle(CommonCommandResult result, CommandResultContext commandContext, ExecutingContext executingContext);
    }
    
    
}