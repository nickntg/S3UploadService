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
        void UploadFile(ConfigEntry configEntry, string fileName, Guid randomGuid);
        void UploadFile(ConfigEntry configEntry, string fileName, string contents, Guid randomGuid);
    }

    public class S3Helper : IS3Helper
    {
        private AmazonS3Client _s3Client;
        private string         _bucket;
 
        public S3Helper(AppSettings settings)
        {
            CreateClient(settings);
        }

        public void UploadFile(ConfigEntry configEntry, string fileName, string contents, Guid randomGuid)
        {
            var result = _s3Client.PutObjectAsync(
                new PutObjectRequest
                {
                    ContentBody = contents,
                    BucketName = _bucket,
                    Key = CreateKey(configEntry, fileName, randomGuid)
                }).Result;

            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"S3 upload error ({result.HttpStatusCode})");
            }
        }

        public void UploadFile(ConfigEntry configEntry, string fileName, Guid randomGuid)
        {
            var result = _s3Client.PutObjectAsync(
                new PutObjectRequest
                {
                    FilePath = fileName,
                    BucketName = _bucket,
                    Key = CreateKey(configEntry, fileName, randomGuid)
                }).Result;

            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"S3 upload error ({result.HttpStatusCode})");
            }
        }

        private string CreateKey(ConfigEntry configEntry, string fileName, Guid randomGuid)
        {
            var start = $"{configEntry.StartKey}/";
            if (configEntry.PrependDate)
            {
                start = $"{start}{DateTime.UtcNow:yyyy/MM/dd}/";
            }
            var replaced = $"{fileName.Replace($"{configEntry.WatchFolder}{Path.DirectorySeparatorChar}", string.Empty).Replace("\\", "/")}";
            if (configEntry.AddRandomGuidToFiles)
            {
                var fi = new FileInfo(fileName);
                var guid = Guid.NewGuid().ToString("N");
                replaced = replaced.Replace(fi.Name, $"{guid}.{fi.Name}");
            }

            if (configEntry.AddRandomGuidToLeafDir)
            {
                var index = replaced.LastIndexOf("/", StringComparison.Ordinal);
                if (index > 0)
                {
                    replaced = replaced.Insert(index, $"/{randomGuid}");
                }
            }

            return $"{start}{replaced}";
        }

        private void CreateClient(AppSettings settings)
        {
            _s3Client = new AmazonS3Client(
                new BasicAWSCredentials(settings.S3AccessKey, settings.S3SecretKey),
                RegionEndpoint.GetBySystemName(settings.Region));
            _bucket = settings.Bucket;
        }
    }
}