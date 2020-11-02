using System;
using System.IO;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace S3UploadService
{
    public interface IS3Helper
    {
        void UploadFile(string fileName);
    }

    public class S3Helper : IS3Helper
    {
        private AmazonS3Client _s3Client;
        private string         _bucket;
        private string         _startKey;
        private string         _pathToIgnore;

        public S3Helper(AppSettings settings)
        {
            CreateClient(settings);
        }

        public void UploadFile(string fileName)
        {
            var result = _s3Client.PutObjectAsync(
                new PutObjectRequest
                {
                    FilePath = fileName,
                    BucketName = _bucket,
                    Key = $"{_startKey}/{DateTime.UtcNow:yyyy-MM-dd}/{fileName.Replace(_pathToIgnore, string.Empty).Replace("\\", "/")}"
                }).Result;

            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"S3 upload error ({result.HttpStatusCode})");
            }
        }

        private void CreateClient(AppSettings settings)
        {
            _s3Client = new AmazonS3Client(
                new BasicAWSCredentials(settings.S3AccessKey, settings.S3SecretKey),
                RegionEndpoint.GetBySystemName(settings.Region));
            _bucket = settings.Bucket;
            _startKey = settings.StartKey;
            _pathToIgnore = $"{settings.LookFolder}{Path.DirectorySeparatorChar}";
        }
    }
}
