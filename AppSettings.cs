namespace S3UploadService
{
    public class AppSettings
    {
        public string ConfigFile { get; set; }
        public int InactivityAlertInSeconds { get; set; }
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpUserName { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpFrom { get; set; }
        public string SmtpTo { get; set; }
        public string InactivityAlertSilenceStartTime { get; set; }
        public string InactivityAlertSilenceEndTime { get; set; }
    }
}