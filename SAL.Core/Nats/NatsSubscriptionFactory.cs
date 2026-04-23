using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NATS.Client.JetStream;
using SAL.Core.Processors;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Rabbit.Subscription;
using EventInfo = SAL.Core.Rabbit.Subscription.EventInfo;

namespace SAL.Core.Nats
{
    /// <summary>
    /// Фабрика подписок для NATS JetStream
    /// Реализует интерфейс ISubscriptionFactory
    /// </summary>
    public class NatsSubscriptionFactory : ISubscriptionFactory
    {
        private readonly IJetStream jetStream;
        private readonly ILogger logger;
        private readonly string contourName;

        public NatsSubscriptionFactory(IJetStream jetStream, ILogger logger, string contourName)
        {
            this.jetStream = jetStream;
            this.logger = logger;
            this.contourName = contourName;
        }

        public ISubscription CreateEvent(
            ushort prefetchCount, 
            IEnumerable<EventInfo> eventInfos, 
            Func<RabbitMessage, Action, Action, Task> handler, 
            string subscriptionName = "Event")
        {
            return new NatsSubscription(jetStream, logger, contourName, subscriptionName, eventInfos, handler);
        }

        public ISubscription CreateCommandResult(
            ushort globalPrefetchCount, 
            ushort instancePrefetchCount, 
            ushort typePrefetchCount, 
            Func<RabbitMessage, Action, Action, Task> handler, 
            string subscriptionName = "CommandResults")
        {
            return new NatsSubscription(jetStream, logger, contourName, subscriptionName, null, handler);
        }

        public ISubscription CreateSyncCommandResult(
            ushort syncPrefetchCount, 
            Func<RabbitMessage, Action, Action, Task> handler, 
            string subscriptionName = "SyncCommandResults")
        {
            return new NatsSubscription(jetStream, logger, contourName, subscriptionName, null, handler);
        }

        public ISubscription CreateCommand(
            ushort globalPrefetchCount, 
            ushort mainPrefetchCount, 
            CommandInfo[] commands, 
            Func<RabbitMessage, Action, Action, Task> handler, 
            string subscriptionName = "Command")
        {
            return new NatsSubscription(jetStream, logger, contourName, subscriptionName, null, handler);
        }

        public ISubscription CreateExternalHttp(
            ushort globalPrefetchCount, 
            ExternalHttpInfo[] queues,  
            Func<RabbitMessageEx, Action, Action, Task> handler, 
            string subscriptionName = "ExternalHttp")
        {
            logger.LogWarning("ExternalHttp subscriptions are not supported in NATS mode");
            throw new NotSupportedException("ExternalHttp subscriptions require RabbitMQ-specific features");
        }

        public ISubscription CreateCustom(
            ushort globalPrefetchCount, 
            QueueInfo[] queues, 
            Func<RabbitMessageEx, Action, Action, Task> handler, 
            string subscriptionName = "Custom")
        {
            return new NatsSubscription(jetStream, logger, contourName, subscriptionName, null, null, customHandler: handler);
        }

        public (ISubscription PrivateSubscription, ISubscription SharedSubscription) CreateCommonSharedCommandResult(
            CommonSharedCommandResultConfig config, 
            Func<RabbitMessageEx, Action, Action, Task> handler)
        {
            logger.LogWarning("CommonSharedCommandResult subscriptions are not fully supported in NATS mode");
            var subscription = new NatsSubscription(jetStream, logger, contourName, "CommonShared", null, null, customHandler: handler);
            return (subscription, subscription);
        }
    }
}
