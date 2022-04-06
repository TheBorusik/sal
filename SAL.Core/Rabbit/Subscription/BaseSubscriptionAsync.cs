using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SAL.API;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit.Subscription
{
    public abstract class BaseSubscriptionAsync<T> where T : RabbitMessage
    {
        protected ILogger logger;

        public string SubscriptionName { get; private set; }
        protected readonly IRMQTransport Transport;
        protected int CurrentThread;
        protected IModel Model;
        protected object ModelLocker = new object();
        protected readonly Func<T, Action, Action, Task> Handler;
        protected bool Started;


        protected BaseSubscriptionAsync(
            IRMQTransport transport,
            string subscriptionName,
            Func<T, Action, Action, Task> handler
        )
        {
            Transport = transport;
            SubscriptionName = subscriptionName;
            Handler = handler;
        }

        protected virtual async Task ConsumerOnReceived(object sender, BasicDeliverEventArgs args)
        {
            var consumerTag = await GetConsumerTag(sender);
            var busMessage = await Transform(args, consumerTag);
            await Task.Yield();
            var _ =Task.Run(async () =>
            {
                Interlocked.Increment(ref CurrentThread);
                var acknowledgment = false;
                try
                {

                    //  logger?.Trace($"Принято сообщение CID:{args.BasicProperties.CorrelationId} из {busMessage.QueueName} ex:{args.Exchange}");
                    await Handler(busMessage, 
                        () =>
                        {
                            if (acknowledgment)
                                return;
                            lock (ModelLocker)
                            {
                                Model.BasicAck(args.DeliveryTag, false);
                                acknowledgment = true;
                            }
                        },
                        () =>
                        {
                            if (acknowledgment)
                                return;
                            lock (ModelLocker)
                            {
                                Model.BasicNack(args.DeliveryTag, false, false);
                                acknowledgment = true;
                            }
                        }  
                    );
                }
                catch (Exception ex)
                {
                    logger?.Critical($"При обработке сообщения CID:{args.BasicProperties.CorrelationId}  произошла ошибка", ex);

                    if (!acknowledgment)
                    {
                        lock (ModelLocker)
                        {
                            Model.BasicNack(args.DeliveryTag, false, true);
                        }
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref CurrentThread);
                }
            });
        }


        protected abstract Task<string> GetConsumerTag(object consumer);
        protected abstract Task<T> Transform(BasicDeliverEventArgs args, string consumerTag);
    }
}