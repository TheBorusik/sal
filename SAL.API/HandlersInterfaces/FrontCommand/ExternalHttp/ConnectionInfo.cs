using System.Net;

namespace SAL.API.FrontCommand
{
    public class ConnectionInfo
    {
        public IPAddress RemoteIpAddress { get; set; }
        public int RemotePort { get; set; } 
    }
}