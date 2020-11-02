namespace S3UploadService
{
    public class AppSettings
    {
        public string LookFolder { get; set; }
        public string DoneFolder { get; set; }
        public string FailedFolder { get; set; }
        public int MaxRetries { get; set; }
        public string S3AccessKey { get; set; }
        public string S3SecretKey { get; set; }
        public string Region { get; set; }
        public string Bucket { get; set; }
        public string StartKey { get; set; }
        public int WakeupSeconds { get; set; }
        public int GraceSeconds { get; set; }
        public string FileMask { get; set; }
    }
}