using System;
using System.Collections.Generic;

namespace S3UploadService
{
    public interface IUploadObserver
    {
        void FileUploaded(string name);
        DateTime LastUploadTime { get; set; }
        Dictionary<string, int> UploadCounts { get; set; }
    }

    public class UploadObserver : IUploadObserver
    {
        public DateTime LastUploadTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, int> UploadCounts { get; set; } = new Dictionary<string, int>();

        public void FileUploaded(string name)
        {
            lock (UploadCounts)
            {
                if (!UploadCounts.ContainsKey(name))
                {
                    UploadCounts.Add(name, 0);
                }

                UploadCounts[name]++;
            }
            LastUploadTime = DateTime.UtcNow;
        }
    }
}
