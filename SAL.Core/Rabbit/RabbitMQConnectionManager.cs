using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Config.Rabbit;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.EventArgs;
using SAL.API;

namespace SAL.Core.Rabbit
{
    public class RabbitMQConnectionManager : IDisposable
    {
        private IConnection connection;
        private CancellationTokenSource tokenSource;
        private Task connectionTask = Task.CompletedTask;

        public event EventHandler<ConnectionFailureEventArgs> ConnectionFailure;
        public event EventHandler<ConnectionRestoreEventArgs> ConnectionRestore;

        private readonly RabbitConfig config;
        private readonly ILogger logger;
        private readonly ConnectionFactory factory;


        public RabbitMQConnectionManager(RabbitConfig rabbitConfig, ILoggerProvider loggerProvider)
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
                Protocol = Protocols.DefaultProtocol,
                HostName = config.Host,
                Port = config.Port,
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            ContourName = config.VirtualHost.ToUpper();
        }

        public void Start()
        {
            StartConnection();
        }

        public void Stop()
        {
            tokenSource?.Cancel();
            connectionTask?.Wait();
            connection?.Close();
            connection?.Dispose();
            connection = null;
        }

        public IModel CreateModel()
        {
            if (connection == null)
                throw new NoConnectionException();
            return connection?.CreateModel();
        }

        private void StartConnection()
        {
            if (connection != null)
                return;

            tokenSource = new CancellationTokenSource();

            if (connectionTask.Status == TaskStatus.RanToCompletion)
            {
                connectionTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        if (tokenSource.IsCancellationRequested)
                            break;

                        try
                        {

                            connection = factory.CreateConnection();
                            connection.ConnectionShutdown += OnConnectionShutdown;
                            connection.RecoverySucceeded += ConnectionOnRecoverySucceeded;
                            OnConnectionRestore(new ConnectionRestoreEventArgs());
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.Warning($"Ошибка соединения с шиной {config.Host}/{config.VirtualHost}", ex);
                        }

                        try
                        {
                            await Task.Delay(config.RetryTimeout, tokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }

                    }
                });
            }
        }


        private void ConnectionOnRecoverySucceeded(object sender, System.EventArgs e)
        {
            logger.Info($"Установлено соединение с шиной {config.Host}/{config.VirtualHost}");
            OnConnectionRestore(new ConnectionRestoreEventArgs());

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