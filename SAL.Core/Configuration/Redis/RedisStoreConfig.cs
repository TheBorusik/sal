using System.Security.Authentication;

namespace SAL.Core.Configuration.Redis
{
    public class RedisStoreConfig
    {
        public RedisConfig Redis { get; set; }
        public int? SessionDatabase { get; set; }
    }

    public class RedisConfig
    {
        public bool AbortOnConnectFail { get; set; } = true;
        public bool AllowAdmin { get; set; } = false;
        public string ChannelPrefix { get; set; } = null;
        public bool CheckCertificateRevocation { get; set; } = true;
        public int ConnectRetry { get; set; } = 3;
        public int ConnectTimeout { get; set; } = 5000;
        public int? DefaultDatabase { get; set; } = null;
        public int KeepAlive { get; set; } = -1;
        public string ClientName { get; set; } = null;
        public string Password { get; set; } = null;
        public string User { get; set; } = null;
        public string ServiceName { get; set; } = null;
        public bool ResolveDns { get; set; } = false;
        public bool Ssl { get; set; } = false;
        public string SslHost { get; set; } = null;
        public SslProtocols? SslProtocols { get; set; } = null;
        public int SyncTimeout { get; set; } = 5000;
        public int AsyncTimeout { get; set; } = 5000;
        public RedisEndpoint[] Endpoints { get; set; } = new RedisEndpoint[0];
    }

    public class RedisEndpoint
    {
        public string Host { get; set; }
        public int Port { get; set; } = 6379;
    }
}