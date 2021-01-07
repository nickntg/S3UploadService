using System;

namespace S3UploadService
{
    public interface IUploadObserver
    {
        void FileUploaded();
        DateTime LastUploadTime();
    }

    public class UploadObserver : IUploadObserver
    {
        private DateTime _lastDateTime = DateTime.UtcNow;

        public void FileUploaded()
        {
            _lastDateTime = DateTime.UtcNow;
        }

        public DateTime LastUploadTime()
        {
            return _lastDateTime;
        }
    }
}
