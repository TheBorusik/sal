using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SAL.API;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit.Subscription
{
    public class MultiConsumerSubscriptionAsync : BaseSubscriptionAsync<RabbitMessage>, ISubscription
    {
        private readonly List<QueueDataAsync> queueDatas;
        private readonly ushort globalPrefetchCount;

        public MultiConsumerSubscriptionAsync(IRMQTransport transport, string subscriptionName, ushort globalPrefetchCount, QueueInfo[] queueInfos, Func<RabbitMessage, Action, Action, Task> handler)
            : base(transport, subscriptionName, handler)
        {
            logger = transport.CreateLogger($"RMQ.{SubscriptionName}");
            this.globalPrefetchCount = globalPrefetchCount;

            this.queueDatas = queueInfos.Select(s => new QueueDataAsync
            {
                QueueName = s.QueueName,
                PrefetchCount = s.PrefetchCount,
            }).ToList();
        }

        protected override Task<string> GetConsumerTag(object consumer)
        {
            if (consumer is AsyncDefaultBasicConsumer defaultConsumer)
                return Task.FromResult(defaultConsumer.ConsumerTags.FirstOrDefault());
            return Task.FromResult(string.Empty);
        }

        protected override Task<RabbitMessage> Transform(BasicDeliverEventArgs args, string consumerTag)
        {
            return Task.FromResult(new RabbitMessage
            {
                Payload = args.Body.ToArray(),
                TimeStamp = args.BasicProperties.Timestamp.ToDateTime(),
                CorrelationId = args.BasicProperties.CorrelationId,
                Priority = args.BasicProperties.Priority,
                RoutingKey = args.RoutingKey,
                Exchange = args.Exchange,
                QueueName = queueDatas.FirstOrDefault(x => x.ConsumerTag == consumerTag)?.QueueName
            });
        }

        public void Start()
        {
            logger.Info("Запуск обработки");
            lock (ModelLocker)
            {
                if (Started)
                    return;
                if (Model?.IsClosed ?? true)
                {
                    if (Model != null)
                    {
                        Model.Close();
                        Model.Dispose();
                    }

                    Model = Transport.CreateModel();
                }
                if(globalPrefetchCount > 0)
                    Model.BasicQos(0, globalPrefetchCount, true);

                foreach(var queueData in queueDatas)
                {
                    if(queueData.PrefetchCount > 0)
                        Model.BasicQos(0, queueData.PrefetchCount, false);


                    queueData.Consumer = new AsyncEventingBasicConsumer(Model);
                    queueData.Consumer.Received += ConsumerOnReceived;
                    queueData.ConsumerTag = Model.BasicConsume(queueData.QueueName, false, queueData.Consumer);
                    
                }

                Started = true;
            }
        }

        public void Stop()
        {
            logger.Info("Остановка обработки");
            lock (ModelLocker)
            {
                if (!Started)
                    return;
                foreach(var queueData in queueDatas)
                {
                    try
                    {
                        if (Model.IsOpen)
                        {
                            Model.BasicCancel(queueData.ConsumerTag);
                            queueData.ConsumerTag = string.Empty;
                            queueData.Consumer = null;
                        }
                    }
                    catch (Exception)
                    {
                        //    throw;
                    }
                }

                Started = false;
            }

            while (CurrentThread > 0)
            {
                logger.Debug($"Кол-во обрабатываемых задач {CurrentThread} - ждем завершения");
                Thread.Sleep(1000);
            }

            logger.Debug($"Все обрабатываемых задачи завершились.");
        }

        public void Dispose()
        {
            Stop();
            Model?.Close();
            Model?.Dispose();
        }
    }
}