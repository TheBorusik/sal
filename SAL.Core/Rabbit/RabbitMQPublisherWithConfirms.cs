using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using RabbitMQ.Client;
using SAL.API;
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
        
        private readonly long maxMessageSize = 100 * 1024 * 1024;
        private readonly long maxMessageWarningSize = 10 * 1024 * 1024;


        private IModel rmqChannel;
        private LinkedList<ConfirmsItem> waitConfirmItems = new();
        private object openLocker = new();


        public RabbitMQPublisherWithConfirms(IRMQTransport transport, ILoggerProvider loggerProvider)
        {
            this.transport = transport;
            logger = loggerProvider.CreateLogger("RMQ.Publisher");
        }


        public async Task PublishAsync(RabbitMessage msg)
        {
            if (!transport.IsConnected)
                throw new NoConnectionException();

            if (msg.Payload.Length > maxMessageSize)
                throw new MessageIsTooLongException();
            
            if (msg.Payload.Length > maxMessageWarningSize)
                logger.Warning($"Сообщение cid:{msg.CorrelationId}, ex:{msg.Exchange}, rk:{msg.RoutingKey}, size:{msg.Payload.Length.HumanReadable()} привысил размер сообщения {maxMessageWarningSize.HumanReadable()} ");

            if (rmqChannel.IsClosed)
                Open();

            var props = rmqChannel.CreateBasicProperties();
            props.DeliveryMode = 2;
            props.CorrelationId = msg.CorrelationId;
            props.Priority = msg.Priority;
            props.Timestamp = msg.TimeStamp.ToAmqp();

            var item = new ConfirmsItem
            {
                Source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
            };

            try
            {
                lock (waitConfirmItems)
                {
                    item.SequenceNumber = rmqChannel.NextPublishSeqNo;
                    rmqChannel.BasicPublish(msg.Exchange, msg.RoutingKey, props, msg.Payload);
                    waitConfirmItems.AddLast(item);
                }
            }
            catch (Exception e)
            {
                logger.Error(e.ToDto());
                throw;
            }


            await item.Source.Task;
        }


        public void Start()
        {
            Open();
        }


        public void Stop()
        {
            rmqChannel?.Close();
            rmqChannel?.Dispose();
            lock (waitConfirmItems)
            {
                var item = waitConfirmItems.First;

                while (item != null)
                {
                    item.Value.Source.TrySetException(new MessagePublishedNotConfirmedException());

                    item = item.Next;
                }

                waitConfirmItems.Clear();
            }

            rmqChannel = null;
        }


        public void Open()
        {
            logger.Info("Create channel for publish");
            lock (openLocker)
            {
                if (rmqChannel != null)
                {
                    if (rmqChannel.IsOpen)
                        return;

                    Stop();
                }

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
                            item.Value.Source.TrySetException(new MessageNotPublishedException("Rabbit Nack"));
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
                            item.Value.Source.TrySetException(new MessageNotPublishedException("Rabbit Nack"));

                        item = item.Next;

                        if (n.Multiple)
                            waitConfirmItems.RemoveFirst();
                    }
                };

                rmqChannel.CallbackException += (s, args) =>
                {
                    int x = 0;
                    x++;
                };
                

                rmqChannel.ModelShutdown += (s, args) =>
                {
                    lock (waitConfirmItems)
                    {
                        var item = waitConfirmItems.First;
                        while (item != null)
                        {
                            item.Value.Source.TrySetException(new MessageNotPublishedException(args.ReplyText.ToString()));
                            item = item.Next;
                        }
                        waitConfirmItems.Clear();
                    }
                };


            }
        }
    }
}