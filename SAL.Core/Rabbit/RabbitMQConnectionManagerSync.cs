using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.API;
using SAL.Core.Configuration.Rabbit;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.EventArgs;

namespace SAL.Core.Rabbit
{
    public class RabbitMQConnectionManagerSync : IDisposable
    {
        private IConnection connection;
        
        public event EventHandler<ConnectionFailureEventArgs> ConnectionFailure;
        public event EventHandler<ConnectionRestoreEventArgs> ConnectionRestore;

        private readonly RabbitConfig config;
        private readonly ILogger logger;
        private readonly ConnectionFactory factory;


        public RabbitMQConnectionManagerSync(RabbitConfig rabbitConfig, ILoggerProvider loggerProvider)
        {
            if (rabbitConfig == null)
                throw new ArgumentNullException(nameof(rabbitConfig));

            if (string.IsNullOrWhiteSpace(rabbitConfig.Host))
                throw new ArgumentException("rabbitConfig.Host");

            if (rabbitConfig.RetryTimeout < 100)
                throw new ArgumentException("rabbitConfig.RetryTimeout");

            config = rabbitConfig;
            this.logger = loggerProvider.CreateLogger("RMQ.CM");
            
            factory = new ConnectionFactory
            {
                UserName = config.Username,
                Password = config.Password,
                VirtualHost = config.VirtualHost,
                HostName = config.Host,
                Port = config.Port,
                AutomaticRecoveryEnabled = false,
                TopologyRecoveryEnabled = false
            };
            ContourName = config.VirtualHost.ToUpper();
        }

        public void Start()
        {
            connection = factory.CreateConnection();
            connection.ConnectionShutdown += OnConnectionShutdown;
        }

        public void Stop()
        {
            try
            {
                connection?.Close();
                connection?.Dispose();

            }
            catch (Exception e)
            {
                //
            }
            connection = null;

        }

        public IModel CreateModel()
        {
            if (connection == null)
                throw new NoConnectionException();
            return connection?.CreateModel();
        }

        
        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (e.Initiator == ShutdownInitiator.Application) return;
            logger.Info($"Cоединение с шиной {config.Host}/{config.VirtualHost} прерванно");
            OnConnectionFailure(new ConnectionFailureEventArgs(e.ToString()));
        }
        
        private void OnConnectionRestore(ConnectionRestoreEventArgs e)
        {
            try
            {
                var handler = ConnectionRestore;
                handler?.Invoke(this, e);
            }
            catch (Exception)
            {
                //
            }
        }

        private void OnConnectionFailure(ConnectionFailureEventArgs e)
        {
            try
            {
                var handler = ConnectionFailure;
                handler?.Invoke(this, e);
            }
            catch (Exception)
            {
                //
            }
        }

        public void Dispose()
        {
            Stop();
        }

        public string ContourName { get; }
    }
}