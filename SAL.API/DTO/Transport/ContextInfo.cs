using System.Security.Cryptography.X509Certificates;

namespace SAL.API
{
    public class  BaseContextInfo
    {
        public string SessionId { get; set; }
        public long? AuthId { get; set; }
        public long? ProcessId { get; set; }
        public string OperationId { get; set; }
    }

}