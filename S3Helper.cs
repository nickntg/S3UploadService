using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, AmazonS3Client> _clients;
        private readonly IUploadObserver                    _uploadObserver;
 
        public S3Helper(IUploadObserver uploadObserver)
        {
            _uploadObserver = uploadObserver;
            _clients = new Dictionary<string, AmazonS3Client>();
        }

        public void UploadFile(ConfigEntry configEntry, string fileName, string contents, Guid randomGuid)
        {
            var client = CreateClient(configEntry);
            var result = client.PutObjectAsync(
                new PutObjectRequest
                {
                    ContentBody = contents,
                    BucketName = configEntry.S3Bucket,
                    Key = CreateKey(configEntry, fileName, randomGuid)
                }).Result;

            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"S3 upload error ({result.HttpStatusCode})");
            }

            _uploadObserver.FileUploaded(configEntry.Name);
        }

        public void UploadFile(ConfigEntry configEntry, string fileName, Guid randomGuid)
        {
            var client = CreateClient(configEntry);
            var result = client.PutObjectAsync(
                new PutObjectRequest
                {
                    FilePath = fileName,
                    BucketName = configEntry.S3Bucket,
                    Key = CreateKey(configEntry, fileName, randomGuid)
                }).Result;

            if (result.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"S3 upload error ({result.HttpStatusCode})");
            }

            _uploadObserver.FileUploaded(configEntry.Name);
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

        private AmazonS3Client CreateClient(ConfigEntry configEntry)
        {
            var key = $"{configEntry.S3AccessKey}_{configEntry.S3AccessKey}_{configEntry.S3Region}_{configEntry.S3Bucket}";
            if (!_clients.ContainsKey(key))
            {
                _clients.Add(key, new AmazonS3Client(
                    new BasicAWSCredentials(configEntry.S3AccessKey, configEntry.S3SecretKey),
                    RegionEndpoint.GetBySystemName(configEntry.S3Region)));
            }

            return _clients[key];
        }
    }
}