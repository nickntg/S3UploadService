using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace S3UploadService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker>   _logger;
        private readonly AppSettings       _settings;
        private readonly IS3Helper         _s3Helper;
        private readonly List<FileDetails> _toProcess;

        public Worker(ILogger<Worker> logger, AppSettings settings, IS3Helper s3Helper)
        {
            _logger = logger;
            _settings = settings;
            _s3Helper = s3Helper;
            _toProcess = new List<FileDetails>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogTrace($"Waiting for {_settings.WakeupSeconds} seconds");
                await Task.Delay(_settings.WakeupSeconds*1000, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    FindFiles();

                    ProcessFiles();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unexpected exception");
                }
            }
        }

        private void FindFiles()
        {
            var files = Directory.GetFiles(_settings.LookFolder, _settings.FileMask, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (_toProcess.FirstOrDefault(x => x.FileName == file) == null)
                {
                    _logger.LogDebug($"Adding file {file} to process list");
                    _toProcess.Add(new FileDetails(file));
                }
            }
        }

        private void ProcessFiles()
        {
            var processed = new List<FileDetails>();

            foreach (var file in _toProcess)
            {
                if (file.Added.AddSeconds(_settings.GraceSeconds).CompareTo(DateTime.UtcNow) > 0)
                {
                    continue;
                }

                _logger.LogDebug($"Uploading file {file.FileName}, retry #{file.Retries}");
                if (ProcessFile(file))
                {
                    _logger.LogDebug($"Upload of file {file.FileName} complete");
                    MoveFile(file, _settings.DoneFolder);

                    processed.Add(file);
                }
                else
                {
                    _logger.LogWarning($"Upload of file {file.FileName} failed");
                    file.Retries++;

                    if (_settings.MaxRetries > file.Retries)
                    {
                        _logger.LogWarning($"Upload of file {file.FileName} reached max retries, moving to Failed folder");
                        MoveFile(file, _settings.FailedFolder);

                        processed.Add(file);
                    }
                }
            }

            _toProcess.RemoveAll(details => processed.FirstOrDefault(x => x.FileName == details.FileName) != null);
        }

        private bool ProcessFile(FileDetails file)
        {
            try
            {
                _s3Helper.UploadFile(file.FileName);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error uploading {file.FileName}");
                return false;
            }
            
        }

        private void MoveFile(FileDetails file, string whichFolder)
        {
            if (string.IsNullOrEmpty(whichFolder))
            {
                _logger.LogDebug($"Deleting file {file.FileName}");
                File.Delete(file.FileName);
            }
            else
            {
                _logger.LogDebug($"Moving file {file.FileName} to Done folder");

                MoveFile(
                    file.FileName,
                    Path.Combine(whichFolder,
                        file.FileName.Replace($"{_settings.LookFolder}{Path.DirectorySeparatorChar}", string.Empty)));
            }
        }

        private void MoveFile(string source, string target)
        {
            var targetDir = new FileInfo(target).DirectoryName;
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Move(source, target, true);
        }
    }
}