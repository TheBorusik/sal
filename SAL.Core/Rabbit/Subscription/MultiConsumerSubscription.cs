using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SAL.API.LoggerHelper;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit.Subscription
{
    public class MultiConsumerSubscription : BaseSubscription, ISubscription
    {
        private readonly List<QueueData> queueDatas;
        private readonly ushort globalPrefetchCount;




        public MultiConsumerSubscription(RabbitMQTransport transport, string subscriptionName, ushort globalPrefetchCount, QueueInfo[] queueInfos, Func<RabbitMessage, Action, Action, Task> handler)
            : base(transport, subscriptionName, handler)
        {

            logger = transport.LoggerProvider.CreateLogger($"RMQ.{SubscriptionName}");
            this.globalPrefetchCount = globalPrefetchCount;

            this.queueDatas = queueInfos.Select(s => new QueueData
            {
                QueueName = s.QueueName,
                ConsumerTag = string.Empty,
                PrefetchCount = s.PrefetchCount
            }).ToList();

        }

        protected override string GetConsumerTag(object consumer)
        {
            if (consumer is DefaultBasicConsumer defaultConsumer)
                return defaultConsumer.ConsumerTag;
            return string.Empty;
        }

        protected override RabbitMessage Transform(BasicDeliverEventArgs args, string consumerTag)
        {
            return new RabbitMessage
            {
                Payload = args.Body,
                TimeStamp = args.BasicProperties.Timestamp.ToDateTime(),
                CorrelationId = args.BasicProperties.CorrelationId,
                Priority = args.BasicProperties.Priority,
                RoutingKey = args.RoutingKey,
                Exchange = args.Exchange,
                QueueName = queueDatas.FirstOrDefault(q => q.ConsumerTag == consumerTag)?.QueueName
            };
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


                    foreach (var queueData in queueDatas)
                    {
                        queueData.Consumer = new EventingBasicConsumer(Model);
                        queueData.Consumer.Received += ConsumerOnReceived;
                    }
                }

                foreach (var queueData in queueDatas)
                {
                    Model.BasicQos(0, queueData.PrefetchCount, false);
                    queueData.ConsumerTag = Model.BasicConsume(queueData.QueueName, false, queueData.Consumer);
                }

                Model.BasicQos(0, globalPrefetchCount, true);

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
                foreach (var queueData in queueDatas)
                {
                    try
                    {
                        if (Model.IsOpen)
                        {
                            if (!string.IsNullOrWhiteSpace(queueData.ConsumerTag))
                                Model.BasicCancel(queueData.ConsumerTag);
                        }
                    }
                    catch (Exception)
                    {
                        //    throw;
                    }
                    queueData.ConsumerTag = string.Empty;
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