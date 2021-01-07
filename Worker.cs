using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace S3UploadService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker>    _logger;
        private readonly AppSettings        _settings;
        private readonly IS3Helper          _s3Helper;
        private readonly IInactivityWatcher _inactivityWatcher;

        public Worker(ILogger<Worker> logger, AppSettings settings, IS3Helper s3Helper, IInactivityWatcher inactivityWatcher)
        {
            _logger = logger;
            _settings = settings;
            _s3Helper = s3Helper;
            _inactivityWatcher = inactivityWatcher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var entries = ReadConfigEntries();
            foreach (var watcher in entries.Select(entry => new SingleWatcher(_logger, _s3Helper, entry)))
            {
                watcher.Start(stoppingToken);
            }

            _inactivityWatcher.Start(stoppingToken);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                Thread.Sleep(5000);
            }
        }

        private List<ConfigEntry> ReadConfigEntries()
        {
            if (_settings == null || string.IsNullOrEmpty(_settings.ConfigFile) || !File.Exists(_settings.ConfigFile))
            {
                throw new InvalidOperationException("Configuration file does not exist");
            }

            return JsonConvert.DeserializeObject<List<ConfigEntry>>(File.ReadAllText(_settings.ConfigFile));
        }
    }
}