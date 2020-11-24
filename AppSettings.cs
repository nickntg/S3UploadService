namespace S3UploadService
{
    public class AppSettings
    {
        public string ConfigFile { get; set; }
        public string S3AccessKey { get; set; }
        public string S3SecretKey { get; set; }
        public string Region { get; set; }
        public string Bucket { get; set; }
    }
}