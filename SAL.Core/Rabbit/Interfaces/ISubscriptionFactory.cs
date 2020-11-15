using System;
using System.Threading.Tasks;
using SAL.Core.Rabbit.Subscription;
using SAL.Core.Rabbit.Topology;

namespace SAL.Core.Rabbit.Interfaces
{
    public interface ISubscriptionFactory
    {
        ISubscription CreateSystemEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName= "SystemEvent");
        ISubscription CreateEvent(ushort prefetchCount, string[] eventNames, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName ="Event");
        ISubscription CreateCommandResult(ushort globalPrefetchCount, ushort instancePrefetchCount, ushort typePrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "CommandResults");
        ISubscription CreateSyncCommandResult(ushort syncPrefetchCount, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "SyncCommandResults");
        ISubscription CreateCommand(ushort globalPrefetchCount, ushort mainPrefetchCount, CommandInfo[] commands, Func<RabbitMessage, Action, Action, Task> handler, string subscriptionName = "Command");
        ISubscription CreateExternalHttp(ushort globalPrefetchCount, ExternalHttpInfo[] queues,  Func<RabbitMessageEx, Action, Action, Task> handler, string subscriptionName = "ExternalHttp");
        ISubscription CreateCustom(ushort globalPrefetchCount, QueueInfo[] queues, Func<RabbitMessageEx, Action, Action, Task> handler, string subscriptionName = "Custom");
        bool AddSystemEvent(string eventName);
        bool AddEvent(string eventName);



    }
}