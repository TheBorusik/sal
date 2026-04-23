using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NATS.Client.JetStream;
using Microsoft.Extensions.Logging;
using SAL.Core.Rabbit.Interfaces;
using SAL.Core.Transport;

namespace SAL.Core.Nats
{
    /// <summary>
    /// Реализация транспорта для NATS JetStream
    /// Совместима с интерфейсом IMessageTransport
    /// </summary>
    public class NatsJetStreamTransport : IMessageTransport
    {
        private readonly IConnection connection;
        private readonly IJetStream jetStream;
        private readonly ILogger logger;
        private readonly string contourName;
        private readonly NatsConfig config;

        private bool isConnected;
        private bool disposed;

        public event EventHandler ConnectionRestore;
        public event EventHandler ConnectionFailure;

        public bool IsConnected => isConnected;
        public string ContourName => contourName;

        public NatsJetStreamTransport(
            string host,
            int port,
            string contourName,
            ILoggerProvider loggerProvider,
            NatsConfig config = null)
        {
            this.contourName = contourName;
            this.config = config ?? new NatsConfig();
            
            logger = loggerProvider.CreateLogger($"NATS.Transport.{contourName}");
            
            try
            {
                var factory = new ConnectionFactory();
                var options = factory.CreateDefaultOptions();
                options.Url = $"{host}:{port}";
                options.ReconnectedEventHandler += (sender, args) =>
                {
                    isConnected = true;
                    logger.LogInformation("NATS connection restored");
                    ConnectionRestore?.Invoke(this, EventArgs.Empty);
                };
                options.DisconnectedEventHandler += (sender, args) =>
                {
                    isConnected = false;
                    logger.LogWarning("NATS connection lost");
                    ConnectionFailure?.Invoke(this, EventArgs.Empty);
                };

                connection = factory.CreateConnection(options);
                jetStream = connection.CreateJetStreamContext();
                isConnected = true;
                
                logger.LogInformation($"NATS JetStream transport initialized for contour {contourName}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize NATS JetStream transport");
                throw;
            }
        }

        public void Start()
        {
            if (!isConnected)
            {
                logger.LogWarning("Attempting to start disconnected NATS transport");
                return;
            }
            
            logger.LogInformation("NATS JetStream transport started");
        }

        public void Stop()
        {
            if (disposed) return;
            
            logger.LogInformation("NATS JetStream transport stopping");
            
            try
            {
                connection?.Dispose();
                isConnected = false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while stopping NATS transport");
            }
        }

        public ISubscriptionFactory CreateSubscriptionFactory()
        {
            if (!isConnected)
                throw new InvalidOperationException("NATS connection is not established");
            
            return new NatsSubscriptionFactory(jetStream, logger, contourName);
        }

        public IPublisher CreatePublisher()
        {
            if (!isConnected)
                throw new InvalidOperationException("NATS connection is not established");
            
            return new NatsPublisher(jetStream, logger, contourName);
        }

        public void Dispose()
        {
            if (disposed) return;
            
            Stop();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Конфигурация для NATS транспорта
    /// </summary>
    public class NatsConfig
    {
        public string StreamName { get; set; } = "SAL_STREAM";
        public bool DurableConsumers { get; set; } = true;
        public int MaxReconnectAttempts { get; set; } = 60;
        public TimeSpan ReconnectWait { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    }
}
