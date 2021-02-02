using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RestSharp;

namespace S3UploadService
{
    public interface IInactivityWatcher
    {
        void Start(CancellationToken stoppingToken);
    }
    
    public class InactivityWatcher : IInactivityWatcher
    {
        private readonly ILogger<InactivityWatcher> _logger;
        private readonly AppSettings                _appSettings;
        private readonly IUploadObserver            _uploadObserver;
        private          DateTime                   _silencePeriod    = DateTime.UtcNow;
        private readonly Dictionary<string, int>    _lastUploadCounts = new Dictionary<string, int>();
        private const    int                        MonitorSeconds    = 60;
        
        public InactivityWatcher(ILogger<InactivityWatcher> logger, AppSettings appSettings, IUploadObserver uploadObserver)
        {
            _logger = logger;
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
            var last = DateTime.UtcNow;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                if (DateTime.UtcNow.Subtract(last).TotalSeconds > MonitorSeconds)
                {
                    Report();
                    last = DateTime.UtcNow;
                }

                if (IsSilencePeriod())
                {
                    continue;
                }

                if (DateTime.UtcNow.Subtract(_uploadObserver.LastUploadTime).TotalSeconds >
                    _appSettings.InactivityAlertInSeconds)
                {
                    SendEmailAlert();
                    _silencePeriod = DateTime.UtcNow.AddMinutes(10);
                }
            }
        }

        private void Report()
        {
            var current = _uploadObserver.UploadCounts.ToArray();
            foreach (var x in current)
            {
                var key = x.Key;
                var value = x.Value;
                if (!_lastUploadCounts.ContainsKey(key))
                {
                    _lastUploadCounts.Add(key, 0);
                }

                var currentValue = value;
                var count = currentValue - _lastUploadCounts[key];
                _lastUploadCounts[key] = currentValue;
                ReportMetric(key, DateTime.UtcNow, count);
            }
        }

        private void ReportMetric(string name, DateTime dt, int count)
        {
            try
            {
                var client = new RestClient(_appSettings.MonitoringUrl);
                var request = new RestRequest("/dataservice/api/data", Method.GET);
                request.AddParameter("parameters", "name,dt,count");
                request.AddParameter("values", $"{name},{dt:yyyy-MM-dd HH:mm:ss},{count}");
                request.AddParameter("name", "simple_count_post");
                request.Timeout = 10;
                var response = client.Execute(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating statistics");
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

                client.Send(new MailMessage(_appSettings.SmtpFrom, _appSettings.SmtpTo, "S3 Uploader Alert",
                    $"More than {TimeSpan.FromSeconds(_appSettings.InactivityAlertInSeconds).TotalMinutes} minutes have elapsed without an invoice being uploaded to S3.\r\n\r\nThis alarm will be disabled for the next ten minutes."));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
            }
        }
    }
}
