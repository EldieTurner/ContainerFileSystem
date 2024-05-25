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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ContainerFileSystemWatcher;
using Microsoft.Extensions.DependencyInjection;

namespace ConsoleFileSystemWatcher.Demo;

internal class Program
{
    static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var folderPath = host.Services.GetRequiredService<IConfiguration>()["FOLDER_TO_WATCH"];
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            Console.WriteLine("Please set the FOLDER_TO_WATCH environment variable.");
            return;
        }
        var watcher = host.Services.GetRequiredService<IContainerFileWatcher>();

        watcher.OnFileChanged += (path, name) =>
        {
            Console.WriteLine($"File changed: {path}/{name}");
        };

        watcher.AddWatch(folderPath, TimeSpan.FromMilliseconds(500));
        Console.WriteLine($"Watching {folderPath} for changes.");
        Console.WriteLine();
        Console.ReadLine();
    }

    static IHostBuilder CreateHostBuilder(string[] args) 
        => Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddContainerFileSystemWatcher();
            })
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddEnvironmentVariables();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
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
      - WATCH_FOLDERS=/watch
    volumes:
      - /path/to/watch1:/watch
```
Environment Variables

    WATCH_FOLDERS: Comma-separated list of directories to watch.
    POLLING_INTERVAL: Polling interval in seconds.

## License

This project is licensed under the MIT License. See the LICENSE file for more information.