using System;
using System.Threading.Tasks;

namespace SAL.Core.Rabbit.Interfaces
{
    public interface ISubscriptionFactory
    {
        ISubscription CreateSystemEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName= "SystemEvent");
        ISubscription CreateEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName ="Event");
        ISubscription CreateCommandResult(ushort globalPrefetchCount, ushort instancePrefetchCount, ushort typePrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "CommandResults");
        ISubscription CreateSyncCommandResult(ushort syncPrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "SyncCommandResults");
        ISubscription CreateCommand(ushort globalPrefetchCount, ushort mainPrefetchCount, CommandInfo[] commands, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Command");


        bool AddSystemEvent(string eventName);
        bool AddEvent(string eventName);



    }
}