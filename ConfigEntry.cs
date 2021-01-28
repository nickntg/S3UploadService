namespace S3UploadService
{
    public class ConfigEntry
    {
        public string WatchFolder { get; set; }
        public string DoneFolder { get; set; }
        public string FailFolder { get; set; }
        public int MaxRetries { get; set; }
        public string StartKey { get; set; }
        public int WakeupSeconds { get; set; }
        public int GraceSeconds { get; set; }
        public string FileMask { get; set; }
        public bool AddRandomGuidToFiles { get; set; }
        public bool AddRandomGuidToLeafDir { get; set; }
        public bool FakeAAndBFiles { get; set; }
        public bool PrependDate { get; set; }
        public string S3AccessKey { get; set; }
        public string S3SecretKey { get; set; }
        public string S3Region { get; set; }
        public string S3Bucket { get; set; }
    }
}
