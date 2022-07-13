using System;

namespace SAL.Core.Processors
{
    public interface IMetricProvider
    {
        void IncCommand(string commandName, TimeSpan elapsed, bool isFailure);
        void IncEvent(string eventName, TimeSpan elapsed, bool isFailure);
        void IncCommandResult(TimeSpan elapsed, bool isFailure);
        void IncSyncCommandResult(TimeSpan elapsed, bool isFailure);
        void IncSharedCommandResult(TimeSpan elapsed, bool isFailure);

        void RegisterCommand(string commandName);
        void RegisterEvent(string eventName);

        
    }
}