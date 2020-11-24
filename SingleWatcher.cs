using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace S3UploadService
{
    public class SingleWatcher
    {
        private readonly ILogger           _logger;
        private readonly IS3Helper         _s3Helper;
        private readonly List<FileDetails> _toProcess;
        private readonly ConfigEntry       _configEntry;

        public SingleWatcher(ILogger logger, IS3Helper s3Helper, ConfigEntry configEntry)
        {
            _logger = logger;
            _s3Helper = s3Helper;
            _configEntry = configEntry;
            _toProcess = new List<FileDetails>();
        }

        public void Start(CancellationToken stoppingToken)
        {
            var task = new Task(async () => await ExecuteAsync(stoppingToken));
            task.Start();
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogTrace($"Waiting for {_configEntry.WakeupSeconds} seconds");
                await Task.Delay(_configEntry.WakeupSeconds * 1000, stoppingToken);

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
            var files = Directory.GetFiles(_configEntry.WatchFolder, _configEntry.FileMask, SearchOption.AllDirectories);
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
                if (file.Added.AddSeconds(_configEntry.GraceSeconds).CompareTo(DateTime.UtcNow) > 0)
                {
                    continue;
                }

                _logger.LogDebug($"Uploading file {file.FileName}, retry #{file.Retries}");
                if (ProcessFile(file))
                {
                    _logger.LogDebug($"Upload of file {file.FileName} complete");
                    MoveFile(file, _configEntry.DoneFolder);

                    processed.Add(file);
                }
                else
                {
                    _logger.LogWarning($"Upload of file {file.FileName} failed");
                    file.Retries++;

                    if (_configEntry.MaxRetries > file.Retries && _configEntry.MaxRetries > 0)
                    {
                        _logger.LogWarning($"Upload of file {file.FileName} reached max retries, moving to Failed folder");
                        MoveFile(file, _configEntry.FailFolder);

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
                _s3Helper.UploadFile(_configEntry, file.FileName);
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
                        file.FileName.Replace($"{_configEntry.WatchFolder}{Path.DirectorySeparatorChar}", string.Empty)));
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
