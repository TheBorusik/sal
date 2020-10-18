using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SAL.API;
using SAL.API.Monad;
using SAL.Core.Rabbit.Helpers;
using SAL.Core.Rabbit.Interfaces;

namespace SAL.Core.Rabbit.Subscription
{
    public class MultiConsumerSubscriptionEx : BaseSubscription<RabbitMessageEx>, ISubscription
    {
        private readonly List<QueueData> queueDatas;
        private readonly ushort globalPrefetchCount;


        public MultiConsumerSubscriptionEx(RabbitMQTransport transport, string subscriptionName, ushort globalPrefetchCount, QueueInfo[] queueInfos, Func<RabbitMessageEx, Action, Action, Task> handler)
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

        protected override RabbitMessageEx Transform(BasicDeliverEventArgs args, string consumerTag)
        {
            var headers = new JObject();

            if (args.BasicProperties.IsHeadersPresent())
            {
                args.BasicProperties.Headers.ForEach(kv =>
                {
                    try
                    {
                        headers.Add(kv.Key, Transform(kv.Value));
                    }
                    catch(Exception ex)
                    {
                        //
                    }
                });
            }

            return new RabbitMessageEx
            {
                Payload = args.Body,
                TimeStamp = args.BasicProperties.Timestamp.ToDateTime(),
                CorrelationId = args.BasicProperties.CorrelationId,
                Priority = args.BasicProperties.Priority,
                RoutingKey = args.RoutingKey,
                Exchange = args.Exchange,
                QueueName = queueDatas.FirstOrDefault(q => q.ConsumerTag == consumerTag)?.QueueName,
                Redelivered = args.Redelivered,
                Headers = headers
            };
        }


        private JToken Transform(object data)
        {
            switch (data)
            {
                case byte[] b:
                    return Transform(b);
                case List<object> li:
                    return Transform(li);
                case long l:
                    return Transform(l);
                case AmqpTimestamp at:
                    return Transform(at);
                case Dictionary<string, object> d:
                    return Transform(d);
                default:
                    throw new Exception($"не обрабатываемай тип {data.GetType().Name}");
            }
        }

        private JValue Transform(byte[] data)
        {
            return new JValue(Encoding.UTF8.GetString(data));
        }

        private JValue Transform(long data)
        {
            return new JValue(data);
        }

        private JValue Transform(AmqpTimestamp amqpTimestamp)
        {
            return new JValue(amqpTimestamp.ToDateTime());
        }

        private JArray Transform(List<object> list)
        {
            var jA = new JArray();
            foreach(var val in list)
            {
                jA.Add(Transform(val));
            }
            return jA;
        }

        private JObject Transform(Dictionary<string, object> dic)
        {
            var obj = new JObject();
            dic.ForEach(kv =>
            {
                obj.Add(kv.Key, Transform(kv.Value));
            });
            return obj;
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

                foreach(var queueData in queueDatas)
                {
                    Model.BasicQos(0, queueData.PrefetchCount, false);

                    if (queueData.Consumer == null)
                    {
                        queueData.Consumer = new EventingBasicConsumer(Model);
                        queueData.Consumer.Received += ConsumerOnReceived;
                    }

                    if (!queueData.Consumer.IsRunning)
                    {
                        queueData.ConsumerTag = Model.BasicConsume(queueData.QueueName, false, queueData.Consumer);
                    }
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
                foreach(var queueData in queueDatas)
                {
                    try
                    {
                        if (Model.IsOpen)
                        {
                            if (!string.IsNullOrWhiteSpace(queueData.ConsumerTag))
                            {
                                Model.BasicCancel(queueData.ConsumerTag);
                                queueData.ConsumerTag = string.Empty;
                            }
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