using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using SAL.Infrastructure;

namespace SAL.API
{
    public static class ExecuteCommandHelper
    {
        public static TCommandResult GetSuccessOrThrow<TCommandResult>(this CommandResult<TCommandResult> commandResult,
            Func<InternalExceptionDTO, TCommandResult> error = null,
            Func<string, JObject, TCommandResult> other = null)
            where TCommandResult : class, ICommandResult, new()
        {
            if (commandResult.ResultCode == ResultCodes.Success)
                return commandResult.GetResult();
            if (commandResult.ResultCode == ResultCodes.Error)
            {
                if (error != null)
                    return error.Invoke(commandResult.Error);
                throw commandResult.Error.ToException();
            }
            if (other != null)
                return other.Invoke(commandResult.ResultCode, commandResult.Result.Clone());
            throw SalError.CreateException(SalErrorCodes.NotSuccess);
        }
        
        public static async Task<TCommandResult> GetSuccessOrThrowAsync<TCommandResult>(this CommonCommandResult commandResult,
            Func<InternalExceptionDTO, Task<TCommandResult>> error = null,
            Func<string, JObject, Task<TCommandResult>> other = null)
        {
            if (commandResult.ResultCode == ResultCodes.Success)
                return commandResult.GetResult<TCommandResult>();
            
            if (commandResult.ResultCode == ResultCodes.Error)
            {
                if (error != null)
                    return await error.Invoke(commandResult.Error);
                throw commandResult.Error.ToException();
            }

            if (other != null)
                return await other.Invoke(commandResult.ResultCode, commandResult.Result.Clone());
            throw SalError.CreateException(SalErrorCodes.NotSuccess);

        }
        
        public static async Task<TCommandResult> GetSuccessOrThrowAsync<TCommandResult>(this SimpleCommandResult simpleResult,
            Func<InternalExceptionDTO, CommandResultContext, Task<TCommandResult>> error = null,
            Func<string, JObject, CommandResultContext, Task<TCommandResult>> other = null)
        {
            if (simpleResult.CommandResult.ResultCode == ResultCodes.Success)
                return simpleResult.CommandResult.GetResult<TCommandResult>();
            
            if (simpleResult.CommandResult.ResultCode == ResultCodes.Error)
            {
                if (error != null)
                    return await error.Invoke(simpleResult.CommandResult.Error, simpleResult.CommandResultContext);
                throw simpleResult.CommandResult.Error.ToException();
            }

            if (other != null)
                return await other.Invoke(simpleResult.CommandResult.ResultCode, simpleResult.CommandResult.Result.Clone(), simpleResult.CommandResultContext);
            throw SalError.CreateException(SalErrorCodes.NotSuccess);

        }
        

        
        
        public static Task ProcessCommandResult<TCommandResult>(this CommandResult<TCommandResult> commandResult, 
            Func<TCommandResult, Task> success,
            Func<InternalExceptionDTO, Task> error = null,
            Func<string, JObject, Task> other = null)
            where TCommandResult : class, ICommandResult, new()
        {
            if (commandResult.ResultCode == ResultCodes.Success)
                return success(commandResult.GetResult());
            if (commandResult.ResultCode == ResultCodes.Error)
                return error?.Invoke(commandResult.Error) ?? Task.CompletedTask;
            return other?.Invoke(commandResult.ResultCode, commandResult.GetRawResult()) ?? Task.CompletedTask;
        }
        
        public static Task ProcessCommandResult(this CommonCommandResult commandResult, 
            Func<JObject, Task> success,
            Func<InternalExceptionDTO, Task> error = null,
            Func<string, JObject, Task> other = null)
        {
            if (commandResult.ResultCode == ResultCodes.Success)
                return success(commandResult.Result.Clone());
            if (commandResult.ResultCode == ResultCodes.Error)
                return error?.Invoke(commandResult.Error) ?? Task.CompletedTask;
            return other?.Invoke(commandResult.ResultCode, commandResult.Result.Clone()) ?? Task.CompletedTask;
        }
        
        public static Task ProcessCommandResult<T>(this CommonCommandResult commandResult, 
            Func<T, Task> success,
            Func<InternalExceptionDTO, Task> error = null,
            Func<string, JObject, Task> other = null)
        {
            if (commandResult.ResultCode == ResultCodes.Success)
                return success(commandResult.GetResult<T>());
            if (commandResult.ResultCode == ResultCodes.Error)
                return error?.Invoke(commandResult.Error) ?? Task.CompletedTask;
            return other?.Invoke(commandResult.ResultCode, commandResult.Result.Clone()) ?? Task.CompletedTask;
        }
        
        public static Task ProcessCommandResult<T>(this SimpleCommandResult simpleResult, 
            Func<T,CommandResultContext, Task> success,
            Func<InternalExceptionDTO,CommandResultContext ,Task> error = null,
            Func<string, JObject, CommandResultContext, Task> other = null)
        {
            if (simpleResult.CommandResult.ResultCode == ResultCodes.Success)
                return success(simpleResult.CommandResult.GetResult<T>(), simpleResult.CommandResultContext);
            if (simpleResult.CommandResult.ResultCode == ResultCodes.Error)
                return error?.Invoke(simpleResult.CommandResult.Error, simpleResult.CommandResultContext) ?? Task.CompletedTask;
            return other?.Invoke(simpleResult.CommandResult.ResultCode, simpleResult.CommandResult.Result.Clone(), simpleResult.CommandResultContext) ?? Task.CompletedTask;
        }
    }
}