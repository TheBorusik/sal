using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.Internal;
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
        }

        public async Task UploadFileAsync(string filePath, string bucketName, string fileId)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);

            var s3Client = CreateClient();

            var listBuckets = await s3Client.ListBucketsAsync();
            if (!listBuckets.Buckets.Exists(m => m.BucketName == bucketName))
            {
                s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucketName
                });
            }

            var fileTransferUtility = new TransferUtility(s3Client);

            await fileTransferUtility.UploadAsync(filePath, bucketName, fileId);
        }

        public async Task DownloadFileAsync(string bucketName, string fileId, string filePath)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);

            var s3Client = CreateClient();

            var fileTransferUtility = new TransferUtility(s3Client);

            await fileTransferUtility.DownloadAsync(filePath, bucketName, fileId);
        }

        private AmazonS3Client CreateClient()
        {
            if (!string.IsNullOrWhiteSpace(config.Region))
            {
                var region = RegionEndpoint.GetBySystemName(config.Region);
                return new AmazonS3Client(config.AccessKey, config.AccessKey, region);
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
        Task UploadFileAsync(string filePath, string bucketName, string fileId);
        Task DownloadFileAsync(string bucketName, string fileId, string filePath);
    }
}