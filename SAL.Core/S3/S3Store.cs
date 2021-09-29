using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Logging;
using SAL.API;
using SAL.Core.Configuration.S3;

namespace SAL.Core.S3
{
    public class S3Store : IS3Store
    {
        private ILogger logger;
        public S3StoreConfig config = null;
        private const string sectionName = "S3Store";

        public S3Store(ILoggerProvider loggerProvider, IConfigWatcher configWatcher)
        {
            config = configWatcher.GetSection(sectionName)?.ConvertValue<S3StoreConfig>();
            logger = loggerProvider.CreateLogger("S3Store");
        }

        public async Task UploadFileAsync(string filePath, string fileId)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);
            

            var s3Client = CreateClient();

            var listBuckets = await s3Client.ListBucketsAsync();
            if (!listBuckets.Buckets.Exists(m => m.BucketName == config.BucketName))
            {
                throw new Exception($"Bucket '{config.BucketName}' Not exist ");
            }

            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(filePath, config.BucketName, fileId);
        }

        public async Task DownloadFileAsync(string fileId, string filePath)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);


            var s3Client = CreateClient();

            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.DownloadAsync(filePath, config.BucketName, fileId);
        }



        private AmazonS3Client CreateClient()
        {
            if (!string.IsNullOrWhiteSpace(config.Region))
            {
                var region = RegionEndpoint.GetBySystemName(config.Region);
                return new AmazonS3Client(config.AccessKey, config.SecretKey, region);
            }

            if (!string.IsNullOrEmpty(config.ServiceUrl))
            {
                var s3ClientConfig = new AmazonS3Config()
                {
                    ServiceURL = config.ServiceUrl,
                    ForcePathStyle = config.ForcePathStyle,
                };

                return new AmazonS3Client(config.AccessKey, config.SecretKey, s3ClientConfig);
            }

            throw new SalNotConfiguredException("S3");
        }
        
        
    }

    public interface IS3Store
    {
        Task UploadFileAsync(string filePath, string fileId);
        Task DownloadFileAsync(string fileId, string filePath);
        
    }
}