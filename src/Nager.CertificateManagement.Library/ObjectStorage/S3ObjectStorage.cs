using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.ObjectStorage
{
    public class S3ObjectStorage : IObjectStorage, IDisposable
    {
        private readonly ILogger<S3ObjectStorage> _logger;
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName = "certificatemanagement";

        public S3ObjectStorage(ILogger<S3ObjectStorage> logger)
        {
            this._logger = logger;

            var config = new AmazonS3Config
            {
                ServiceURL = "http://localhost:9000",
                ForcePathStyle = true
            };

            //TODO:Move config to appsettings
            this._s3Client = new AmazonS3Client("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", config);

            try
            {
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                this.InitializeS3ClientAsync(cancellationTokenSource.Token).GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "Cannot Initialize S3Client");
            }
        }

        public void Dispose()
        {
            this._s3Client?.Dispose();
        }

        private async Task InitializeS3ClientAsync(CancellationToken cancellationToken = default)
        {
            var bucketResponse = await this._s3Client.ListBucketsAsync(cancellationToken);
            if (!bucketResponse.Buckets.Select(o => o.BucketName).Contains(this._bucketName))
            {
                await this._s3Client.PutBucketAsync(this._bucketName, cancellationToken);
            }
        }

        public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await this._s3Client.ListBucketsAsync(cancellationToken);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception exception)
            {
                this._logger.LogError(exception, "S3Client is not ready");
            }

            return false;
        }

        public async Task AddFileAsync(string key, byte[] data, CancellationToken cancellationToken = default)
        {
            using var memoryStream = new MemoryStream(data);

            var request = new PutObjectRequest
            {
                BucketName = this._bucketName,
                InputStream = memoryStream,
                Key = key,
            };

            await this._s3Client.PutObjectAsync(request, cancellationToken);
        }

        public async Task<byte[]> GetFileAsync(string key, CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = this._bucketName,
                Key = key
            };

            using var response = await this._s3Client.GetObjectAsync(request, cancellationToken);

            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }

        public async Task<bool> FileExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var request = new ListObjectsRequest
            {
                BucketName = this._bucketName,
                Prefix = key,
                MaxKeys = 1
            };

            var response = await this._s3Client.ListObjectsAsync(request, cancellationToken);
            return response.S3Objects.Any();
        }
    }
}
