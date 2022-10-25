using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
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

        public async Task DownloadFileAsync(string fileId, string filePath, long? byteLimit = null)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);

            var s3Client = CreateClient();
            if (!await CheckSizeLimitAsync(s3Client, fileId, byteLimit))
                throw new SizeLimitException(byteLimit.Value);

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

        private async Task<bool> CheckSizeLimitAsync(AmazonS3Client s3Client, string fileId, long? byteLimit)
        {
            if (!byteLimit.HasValue)
                return true;

            var fileMetaData = await s3Client.GetObjectMetadataAsync(config.BucketName, fileId);
            var filesize = fileMetaData.Headers.ContentLength;
            if (filesize > byteLimit)
                return false;

            return true;
        }

        public async Task DeleteFileAsync(string fileId)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);

            var s3Client = CreateClient();
            await s3Client.DeleteObjectAsync(config.BucketName, fileId);
        }

        public Task<bool> CheckSizeLimitAsync(string fileId, long byteLimit)
        {
            return CheckSizeLimitAsync(CreateClient(), fileId, byteLimit);
        }

        public async Task<bool> IsFilePresent(string fileId)
        {
            if (config == null)
                throw new SalNotConfiguredException(sectionName);

            var s3Client = CreateClient();

            try
            {
                var fileMetaData = await s3Client.GetObjectMetadataAsync(config.BucketName, fileId);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            
            return false;
        }
    }
}