using Microsoft.Extensions.Logging;

namespace ContainerFileSystemWatcher;

public class ContainerFileWatcher : IContainerFileWatcher
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<ContainerFileWatcher> _logger;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly Task _pollingTask;
    private readonly Dictionary<string, (TimeSpan PollingInterval, Dictionary<string, DateTime> Snapshot)> _watchDirectories;
    private readonly object _lock = new object();

    public event Action<ChangeType, string>? OnFileChanged;

    public ContainerFileWatcher(IFileSystem fileSystem, ILogger<ContainerFileWatcher> logger)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _logger = logger ?? CreateDefaultLogger(); 
        _cancellationTokenSource = new CancellationTokenSource();
        _watchDirectories = new Dictionary<string, (TimeSpan, Dictionary<string, DateTime>)>();
        _pollingTask = Task.Run(() => PollDirectories(_cancellationTokenSource.Token));
    }

    protected ILogger<ContainerFileWatcher> CreateDefaultLogger()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        return loggerFactory.CreateLogger<ContainerFileWatcher>();
    }

    public void AddWatch(string path, TimeSpan pollingInterval)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        if (!_fileSystem.DirectoryExists(path)) throw new DirectoryNotFoundException($"Directory not found: {path}");

        var initialSnapshot = _fileSystem.GetDirectorySnapshot(path);

        lock (_lock)
        {
            _watchDirectories[path] = (pollingInterval, initialSnapshot);
        }

        _logger.LogInformation($"Added watch for {path} with polling interval {pollingInterval.TotalMilliseconds} ms");
    }

    public void RemoveWatch(string path)
    {
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

        lock (_lock)
        {
            _watchDirectories.Remove(path);
        }

        _logger.LogInformation($"Removed watch for {path}");
    }

    protected async Task PollDirectories(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            List<Task> tasks;

            lock (_lock)
            {
                tasks = _watchDirectories.Keys.ToList().Select(directory =>
                {
                    var (pollingInterval, previousSnapshot) = _watchDirectories[directory];
                    if (previousSnapshot == null)
                    {
                        _watchDirectories[directory] = (pollingInterval, _fileSystem.GetDirectorySnapshot(directory));
                        return Task.CompletedTask;
                    }

                    return Task.Run(async () =>
                    {
                        var currentSnapshot = _fileSystem.GetDirectorySnapshot(directory);

                        var addedFiles = currentSnapshot.Keys.Except(previousSnapshot.Keys).ToList();
                        var removedFiles = previousSnapshot.Keys.Except(currentSnapshot.Keys).ToList();
                        var modifiedFiles = currentSnapshot.Keys.Intersect(previousSnapshot.Keys)
                            .Where(file => previousSnapshot[file] != currentSnapshot[file]).ToList();

                        foreach (var file in addedFiles)
                        {
                            _logger.LogInformation($"File created: {file}");
                            OnFileChanged?.Invoke(ChangeType.Created, file);
                        }

                        foreach (var file in removedFiles)
                        {
                            _logger.LogInformation($"File deleted: {file}");
                            OnFileChanged?.Invoke(ChangeType.Deleted, file);
                        }

                        foreach (var file in modifiedFiles)
                        {
                            _logger.LogInformation($"File modified: {file}");
                            OnFileChanged?.Invoke(ChangeType.Modified, file);
                        }

                        lock (_lock)
                        {
                            _watchDirectories[directory] = (pollingInterval, currentSnapshot);
                        }

                        await Task.Delay(pollingInterval, cancellationToken);
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