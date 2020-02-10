using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAL.Core.Config.Rabbit;
using SAL.Core.Exceptions.Rabbit;
using SAL.Core.Rabbit.EventArgs;

namespace SAL.Core.Rabbit
{
    public class RabbitMQConnectionManager : IDisposable
    {
        private IConnection connection;
        private CancellationTokenSource tokenSource;
        private Task connectionTask;

        public event EventHandler<ConnectionFailureEventArgs> ConnectionFailure;
        public event EventHandler<ConnectionRestoreEventArgs> ConnectionRestore;

        private readonly RabbitConfig config;
        private readonly ILogger logger;
        private readonly ConnectionFactory factory;

        public RabbitMQConnectionManager(RabbitConfig rabbitConfig, ILogger logger )
        {
            if (rabbitConfig == null)
                throw new ArgumentNullException(nameof(rabbitConfig));

            if (string.IsNullOrWhiteSpace(rabbitConfig.Host))
                throw new ArgumentException("rabbitConfig.Host");

            if (rabbitConfig.RetryTimeout < 100)
                throw new ArgumentException("rabbitConfig.RetryTimeout");

            factory = new ConnectionFactory();

            config = rabbitConfig;
            this.logger = logger;

            // 
        }

        public void Start()
        {
            StartConnection((c) => Task.CompletedTask);
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

        private void StartConnection(Func<CancellationToken, Task> pre)
        {
            if (connectionTask == null)
            {
                tokenSource = new CancellationTokenSource();

                connectionTask = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        await pre(tokenSource.Token);
                        await RestoreConnectionAsync(tokenSource.Token);

                        OnConnectionRestore(new ConnectionRestoreEventArgs());
                    }

                    finally
                    {
                        connectionTask = null;
                    }
                }, tokenSource.Token);

            }
        }

        private async Task RestoreConnectionAsync(CancellationToken token)
        {

            while (true)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    factory.UserName = config.Username;
                    factory.Password = config.Password;
                    factory.VirtualHost = config.VirtualHost;
                    factory.Protocol = Protocols.DefaultProtocol;
                    factory.HostName = config.Host;
                    factory.Port = config.Port;
                    factory.AutomaticRecoveryEnabled = false;

                    if (connection != null)
                    {
                        if (connection.IsOpen)
                            connection.Close();
                        connection.Dispose();
                        connection = null;
                    }

                    connection = factory.CreateConnection();
                    connection.ConnectionShutdown += OnConnectionShutdown;

                    break;
                }
                catch (Exception ex)
                {
  //
                }

                await Task.Delay(config.RetryTimeout, token);
            }

        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            if (e.Initiator == ShutdownInitiator.Application) return;

            connection.ConnectionShutdown -= OnConnectionShutdown;

            OnConnectionFailure(new ConnectionFailureEventArgs(e.ToString()));

            StartConnection((t) => Task.Delay(config.RetryTimeout, t));
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
    }
}
