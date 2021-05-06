using Microsoft.Extensions.Logging;
using SAL.API;

namespace SAL.Core.Processors
{
    public interface ISalLogger
    {
        void LogIncoming(CommandPayload commandPayload);
        void LogIncoming(CommandResultPayload commandResultPayload);

        void LogOutgoing(CommandPayload commandPayload);
        void LogOutgoing(CommandResultPayload commandResultPayload);

        void LogIncoming(EventPayload eventPayload);
        void LogOutgoing(EventPayload eventPayload);


        void LogNullOutgoing(CommandDescriptor commandDescriptor);
        void LogNullHandler(CommandResultPayload commandResultPayload);

        void LogHandler(CommandResultPayload commandResultPayload, string handlerName, bool handled);

        void LogHandler(CommandPayload commandPayload, string handlerName);

        void LogHandler(EventPayload eventPayload, string handlerName);

        ILogger GetLogger(CommandPayload commandPayload);

        ILogger GetLogger(CommandResultPayload commandResultPayload);

        ILogger GetLogger(EventPayload eventPayload);


    }
}