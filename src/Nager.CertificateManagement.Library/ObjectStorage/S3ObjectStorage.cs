using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nager.CertificateManagement.Library.ObjectStorage
{
    public class S3ObjectStorage : IObjectStorage
    {
        private readonly AmazonS3Client _s3Client;
        private readonly string _bucketName = "certificatemanagement";

        public S3ObjectStorage()
        {
            var config = new AmazonS3Config
            {
                ServiceURL = "http://localhost:9000",
                ForcePathStyle = true
            };

            //TODO:Move config to appsettings
            this._s3Client = new AmazonS3Client("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", config);

            this.InitializeS3ClientAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeS3ClientAsync()
        {
            var bucketResponse = await this._s3Client.ListBucketsAsync();
            if (!bucketResponse.Buckets.Select(o => o.BucketName).Contains(this._bucketName))
            {
                await this._s3Client.PutBucketAsync(this._bucketName);
            }
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
