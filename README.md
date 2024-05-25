# ContainerFileSystemWatcher
A custom 'FileSystemWatcher' that works within a container (to monitor a mounted volume)

## Installation

You can install the `ContainerFileSystemWatcher` package via NuGet:

### .NET CLI

```bash
dotnet add package ContainerFileSystemWatcher
```

### Package-Manager

```bash
Install-Package ContainerFileSystemWatcher
```

## Usage
### Basic Setup

Below is an example of how to set up and use the ContainerFileSystemWatcher in a console application.
`Program.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContainerFileSystemWatcher;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static readonly ConcurrentQueue<string> _fileQueue = new ConcurrentQueue<string>();
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    static async Task Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var watcher = host.Services.GetRequiredService<IContainerFileWatcher>();

        watcher.OnFileChanged += async (changeType, filePath) =>
        {
            if (changeType == ChangeType.Created)
            {
                _fileQueue.Enqueue(filePath);
                await ProcessQueueAsync();
            }
        };

        var watchFolders = Environment.GetEnvironmentVariable("WATCH_FOLDERS")?.Split(',') ?? Array.Empty<string>();
        var pollingIntervalSeconds = int.TryParse(Environment.GetEnvironmentVariable("POLLING_INTERVAL"), out var interval) ? interval : 5;
        var pollingInterval = TimeSpan.FromSeconds(pollingIntervalSeconds);

        foreach (var folderPath in watchFolders)
        {
            if (Directory.Exists(folderPath))
            {
                watcher.AddWatch(folderPath, pollingInterval);
                Console.WriteLine($"Watching {folderPath} for changes every {pollingInterval.TotalSeconds} seconds.");
            }
            else
            {
                Console.WriteLine($"Directory does not exist: {folderPath}");
            }
        }

        Console.WriteLine("Press [Enter] to exit.");
        Console.ReadLine();
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddContainerFileWatcher();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

    private static async Task ProcessQueueAsync()
    {
        if (_semaphore.CurrentCount == 0)
        {
            return; // Already processing a file
        }

        await _semaphore.WaitAsync();

        try
        {
            while (_fileQueue.TryDequeue(out var filePath))
            {
                await ProcessFileAsync(filePath);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task ProcessFileAsync(string filePath)
    {
        Console.WriteLine($"Checking stability of file: {filePath}");
        if (await IsFileStable(filePath))
        {
            Console.WriteLine($"Processing file: {filePath}");
            // Example: Run an ffmpeg command
            var processInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{filePath}\" -codec:v libx264 output.mp4",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = processInfo })
            {
                process.Start();
                await process.WaitForExitAsync();
            }

            Console.WriteLine($"Completed processing file: {filePath}");
        }
        else
        {
            Console.WriteLine($"File is not stable yet: {filePath}");
        }
    }

    private static async Task<bool> IsFileStable(string filePath, int stableTimeMilliseconds = 5000, int checkIntervalMilliseconds = 1000)
    {
        var lastLength = -1L;
        var currentLength = 0L;
        var stableTime = TimeSpan.FromMilliseconds(stableTimeMilliseconds);
        var checkInterval = TimeSpan.FromMilliseconds(checkIntervalMilliseconds);

        while (true)
        {
            try
            {
                currentLength = new FileInfo(filePath).Length;
                if (currentLength == lastLength)
                {
                    await Task.Delay(stableTime);
                    var lengthAfterStableTime = new FileInfo(filePath).Length;
                    if (lengthAfterStableTime == currentLength)
                    {
                        return true;
                    }
                }
                lastLength = currentLength;
                await Task.Delay(checkInterval);
            }
            catch (FileNotFoundException)
            {
                return false;
            }
            catch (IOException)
            {
                // File is in use or other IO error, wait and retry
            }
        }
    }
}
```

## Docker Setup

You can configure the directories to watch and the polling interval via the docker-compose file.
docker-compose.yml

```yaml
version: '3.8'
services:
  filewatcher:
    image: yourimage:latest
    environment:
      - WATCH_FOLDERS=/path/to/watch1,/path/to/watch2
      - POLLING_INTERVAL=5  # Polling interval in seconds
    volumes:
      - /path/to/watch1:/path/to/watch1
      - /path/to/watch2:/path/to/watch2
```
Environment Variables

    WATCH_FOLDERS: Comma-separated list of directories to watch.
    POLLING_INTERVAL: Polling interval in seconds.

## License

This project is licensed under the MIT License. See the LICENSE file for more information.