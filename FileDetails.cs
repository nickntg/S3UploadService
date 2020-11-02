using System;

namespace S3UploadService
{
    public class FileDetails
    {
        public string FileName { get; set; }
        public DateTime Added { get; set; }
        public int Retries { get; set; }

        public FileDetails(string fileName)
        {
            FileName = fileName;
            Added = DateTime.UtcNow;
            Retries = 0;
        }
    }
}