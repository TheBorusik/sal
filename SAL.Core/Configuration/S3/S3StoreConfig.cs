namespace SAL.Core.Configuration.S3
{
    public class S3StoreConfig
    {
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string ServiceUrl { get; set; }
        public bool ForcePathStyle { get; set; }
    }
}