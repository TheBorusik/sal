using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit
{
    public class RabbitMQPublisherWithConfirms : IPublisher
    {
        private class ConfirmsItem
        {
            public ulong SequenceNumber;
            public TaskCompletionSource Source;
        }

        private readonly IRMQTransport transport;
        private readonly ILogger logger;


        private IModel rmqChannel;
        private LinkedList<ConfirmsItem> waitConfirmItems = new();


        public RabbitMQPublisherWithConfirms(IRMQTransport transport, ILoggerProvider loggerProvider)
        {
            this.transport = transport;
            logger = loggerProvider.CreateLogger("RMQ.Publisher");
        }


        public async Task PublishAsync(RabbitMessage msg)
        {
            if (rmqChannel == null)
                throw new NoConnectionException();

            var props = rmqChannel.CreateBasicProperties();
            props.DeliveryMode = 2;
            props.CorrelationId = msg.CorrelationId;
            props.Priority = msg.Priority;
            props.Timestamp = msg.TimeStamp.ToAmqp();

            var item = new ConfirmsItem
            {
                Source = new TaskCompletionSource()
            };


            lock (waitConfirmItems)
            {
                item.SequenceNumber = rmqChannel.NextPublishSeqNo;
                rmqChannel.BasicPublish(msg.Exchange, msg.RoutingKey, props, msg.Payload);
                waitConfirmItems.AddLast(item);
            }

            await item.Source.Task;
            await Task.Yield();
        }


        public void Start()
        {
            if (rmqChannel != null)
                Stop();

            rmqChannel = transport.CreateModel();
            rmqChannel.ConfirmSelect();
            rmqChannel.BasicAcks += (s, a) =>
            {
                LinkedListNode<ConfirmsItem> item;
                lock (waitConfirmItems)
                    item = waitConfirmItems.First;
                
                if (item == null)
                    return;
                if (item.Value.SequenceNumber > a.DeliveryTag)
                    return;
                while (item != null)
                {
                    if (item.Value.SequenceNumber == a.DeliveryTag)
                    {
                        item.Value.Source.TrySetResult();
                        if (a.Multiple)
                            lock (waitConfirmItems)
                                waitConfirmItems.RemoveFirst();
                        else
                        {
                            lock (waitConfirmItems)
                                waitConfirmItems.Remove(item);
                        }

                        break;
                    }

                    if (a.Multiple)
                        item.Value.Source.TrySetResult();

                    item = item.Next;

                    if (!a.Multiple) continue;
                    lock (waitConfirmItems)
                        waitConfirmItems.RemoveFirst();
                }
            };

            rmqChannel.BasicNacks += (s, n) =>
            {
                var item = waitConfirmItems.First;
                if (item == null)
                    return;
                if (item.Value.SequenceNumber > n.DeliveryTag)
                    return;

                while (item != null)
                {
                    if (item.Value.SequenceNumber == n.DeliveryTag)
                    {
                        item.Value.Source.TrySetException(new MessageNotPublishedException());
                        if (n.Multiple)
                            waitConfirmItems.RemoveFirst();
                        else
                        {
                            lock (waitConfirmItems)
                                waitConfirmItems.Remove(item);
                        }
                        break;
                    }

                    if (n.Multiple)
                        item.Value.Source.TrySetException(new MessageNotPublishedException());

                    item = item.Next;

                    if (n.Multiple)
                        waitConfirmItems.RemoveFirst();
                }
            };
        }


        public void Stop()
        {
            rmqChannel?.Dispose();
            rmqChannel = null;
        }
    }
}