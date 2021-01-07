using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace S3UploadService
{
    public interface IInactivityWatcher
    {
        void Start(CancellationToken stoppingToken);
    }
    
    public class InactivityWatcher : IInactivityWatcher
    {
        private readonly AppSettings     _appSettings;
        private readonly IUploadObserver _uploadObserver;
        private          DateTime        _silencePeriod = DateTime.UtcNow;
        
        public InactivityWatcher(AppSettings appSettings, IUploadObserver uploadObserver)
        {
            _appSettings = appSettings;
            _uploadObserver = uploadObserver;
        }

        public void Start(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_appSettings.SmtpServer))
            {
                return;
            }

            var task = new Task(async () => await ExecuteAsync(stoppingToken));
            task.Start();
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                if (IsSilencePeriod())
                {
                    continue;
                }

                if (DateTime.UtcNow.Subtract(_uploadObserver.LastUploadTime()).TotalSeconds >
                    _appSettings.InactivityAlertInSeconds)
                {
                    SendEmailAlert();
                    _silencePeriod = DateTime.UtcNow.AddMinutes(10);
                }
            }
        }

        private bool IsSilencePeriod()
        {
            if (_silencePeriod.CompareTo(DateTime.UtcNow) >= 0)
            {
                return true;
            }

            if (string.IsNullOrEmpty(_appSettings.InactivityAlertSilenceStartTime) ||
                (string.IsNullOrEmpty(_appSettings.InactivityAlertSilenceEndTime)))
            {
                return false;
            }

            var time = DateTime.UtcNow.ToString("HH:mm");
            return String.Compare(time, _appSettings.InactivityAlertSilenceStartTime, StringComparison.Ordinal) >= 0 &&
                   String.Compare(_appSettings.InactivityAlertSilenceEndTime, time, StringComparison.Ordinal) >= 0;
        }

        private void SendEmailAlert()
        {
            try
            {
                var client = new SmtpClient(_appSettings.SmtpServer, _appSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_appSettings.SmtpUserName, _appSettings.SmtpPassword)
                };

                client.Send(new MailMessage(_appSettings.SmtpFrom, _appSettings.SmtpTo, "S3 Uploader Alert", $"More than {TimeSpan.FromSeconds(_appSettings.InactivityAlertInSeconds).TotalMinutes} minutes have elapsed without an invoice being uploaded to S3.\r\n\r\nThis alarm will be disabled for the next ten minutes."));
            }
            catch { }
        }
    }
}
