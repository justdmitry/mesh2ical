using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Mesh2Ical.Yandex
{
    public class StorageService (ILogger<StorageService> logger, IOptionsSnapshot<StorageOptions> options)
    {
        private readonly StorageOptions options = options.Value;

        public async Task Upload(string fileName, MemoryStream data)
        {
            var awsCredentials = new BasicAWSCredentials(options.KeyId, options.KeySecret);
            var awsConfig = new AmazonS3Config
            {
                ServiceURL = options.Endpoint.ToString(),
                AuthenticationRegion = options.Region,
            };

            var awsClient = new AmazonS3Client(awsCredentials, awsConfig);

            var req = new PutObjectRequest()
            {
                BucketName = options.BucketName,
                Key = fileName,
                InputStream = data,
            };

            req.Headers.ContentType = "text/calendar; charset=utf-8";
            req.Headers.CacheControl = "public, max-age=3600"; // 1 hour
            req.Headers.ExpiresUtc = DateTime.UtcNow.AddHours(1);

            var byteCount = data.Length;

            await awsClient.PutObjectAsync(req).ConfigureAwait(false);

            logger.LogInformation("Uploaded {Count} bytes to {FileName}", byteCount, fileName);
        }
    }
}
