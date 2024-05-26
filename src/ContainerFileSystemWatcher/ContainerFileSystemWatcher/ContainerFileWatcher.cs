using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContainerFileSystemWatcher
{
    public class ContainerFileWatcher : IContainerFileWatcher
    {
        private readonly IFileSystemShim _fileSystem;
        private readonly ILogger<ContainerFileWatcher> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _pollingTask;
        private readonly Dictionary<string, WatchDirectory> _watchDirectories;
        private readonly object _lock = new object();

        public bool EnableLogging { get; set; } = true;
        public event Action<ChangeType, string> OnFileChanged;

        public ContainerFileWatcher(IFileSystemShim fileSystem, ILogger<ContainerFileWatcher> logger = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? CreateDefaultLogger();
            _cancellationTokenSource = new CancellationTokenSource();
            _watchDirectories = new Dictionary<string, WatchDirectory>();
            _pollingTask = Task.Run(() => PollDirectories(_cancellationTokenSource.Token));
        }

        protected ILogger<ContainerFileWatcher> CreateDefaultLogger()
        {
            using (var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            }))
            {
                return loggerFactory.CreateLogger<ContainerFileWatcher>();
            }
        }

        public void AddWatch(string path, TimeSpan pollingInterval)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!_fileSystem.DirectoryExists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");

            var initialSnapshot = _fileSystem.GetDirectorySnapshot(path);
            var watchDirectory = new WatchDirectory(path, pollingInterval, initialSnapshot);

            lock (_lock)
            {
                _watchDirectories[path] = watchDirectory;
            }

            if (EnableLogging) _logger.LogInformation($"Added watch for {path} with polling interval {pollingInterval.TotalMilliseconds} ms");
        }

        public void RemoveWatch(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            lock (_lock)
            {
                _watchDirectories.Remove(path);
            }

            if (EnableLogging) _logger.LogInformation($"Removed watch for {path}");
        }

        protected async Task PollDirectories(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                List<Task> tasks;

                lock (_lock)
                {
                    tasks = _watchDirectories.Values.ToList().Select(watchDirectory =>
                    {
                        var previousSnapshot = watchDirectory.Snapshot;
                        if (previousSnapshot == null)
                        {
                            watchDirectory.Snapshot = _fileSystem.GetDirectorySnapshot(watchDirectory.Path);
                            return Task.CompletedTask;
                        }

                        return Task.Run(async () =>
                        {
                            var currentSnapshot = _fileSystem.GetDirectorySnapshot(watchDirectory.Path);

                            var addedFiles = currentSnapshot.Keys.Except(previousSnapshot.Keys).ToList();
                            var removedFiles = previousSnapshot.Keys.Except(currentSnapshot.Keys).ToList();
                            var modifiedFiles = currentSnapshot.Keys.Intersect(previousSnapshot.Keys)
                                .Where(file => previousSnapshot[file] != currentSnapshot[file]).ToList();

                            foreach (var file in addedFiles)
                            {
                                if (EnableLogging) _logger.LogInformation($"File created: {file}");
                                OnFileChanged?.Invoke(ChangeType.Created, file);
                            }

                            foreach (var file in removedFiles)
                            {
                                if (EnableLogging) _logger.LogInformation($"File deleted: {file}");
                                OnFileChanged?.Invoke(ChangeType.Deleted, file);
                            }

                            foreach (var file in modifiedFiles)
                            {
                                if (EnableLogging) _logger.LogInformation($"File modified: {file}");
                                OnFileChanged?.Invoke(ChangeType.Modified, file);
                            }

                            lock (_lock)
                            {
                                watchDirectory.Snapshot = currentSnapshot;
                            }

                            await Task.Delay(watchDirectory.PollingInterval, cancellationToken);
                        });
                    }).ToList();
                }

                await Task.WhenAll(tasks);
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _pollingTask.Wait();
            _cancellationTokenSource.Dispose();
        }
    }
}
