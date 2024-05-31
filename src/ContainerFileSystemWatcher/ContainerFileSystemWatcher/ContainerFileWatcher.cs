using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ContainerFileSystemWatcher
{
    /// <summary>
    /// Implements the <see cref="IContainerFileWatcher"/> interface to watch directories for file changes.
    /// </summary>
    public class ContainerFileWatcher : IContainerFileWatcher, IDisposable
    {
        private readonly IFileSystemShim _fileSystem;
        private readonly ILogger<ContainerFileWatcher> _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _pollingTask;
        private readonly Dictionary<string, WatchDirectory> _watchDirectories;
        private readonly object _lock = new object();

        /// <summary>
        /// Gets or sets a value indicating whether to enable logging.
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        /// <summary>
        /// Occurs when a file in a watched directory changes.
        /// </summary>
        public event Action<ChangeType, string> OnFileChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerFileWatcher"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system shim to use for file system operations.</param>
        /// <param name="logger">The logger to use for logging.</param>
        public ContainerFileWatcher(IFileSystemShim fileSystem, ILogger<ContainerFileWatcher> logger = null)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _logger = logger ?? CreateDefaultLogger();
            _cancellationTokenSource = new CancellationTokenSource();
            _watchDirectories = new Dictionary<string, WatchDirectory>();
            _pollingTask = Task.Run(() => PollDirectories(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Creates a default logger if none is provided.
        /// </summary>
        /// <returns>A logger instance.</returns>
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

        /// <summary>
        /// Adds a directory to the watch list with the specified polling interval.
        /// </summary>
        /// <param name="path">The path of the directory to watch.</param>
        /// <param name="pollingInterval">The interval at which to poll the directory for changes.</param>
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

        /// <summary>
        /// Removes a directory from the watch list.
        /// </summary>
        /// <param name="path">The path of the directory to remove from the watch list.</param>
        public void RemoveWatch(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            lock (_lock)
            {
                _watchDirectories.Remove(path);
            }

            if (EnableLogging) _logger.LogInformation($"Removed watch for {path}");
        }

        /// <summary>
        /// Polls the directories for changes at the specified intervals.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token to cancel the polling.</param>
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

        /// <summary>
        /// Disposes the resources used by the <see cref="ContainerFileWatcher"/> class.
        /// </summary>
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _pollingTask.Wait();
            _cancellationTokenSource.Dispose();
        }
    }

}
