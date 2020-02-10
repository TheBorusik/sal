namespace SAL.Core.Config.Rabbit
{
    public class RabbitConfig
    {
        public string Host { get; set; }
        public int Port { get; set; } = 5672;
        public string VirtualHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int RetryTimeout { get; set; } = 1000;
    }
}
