using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ContainerFileSystemWatcher.ConsoleApp;

internal class Program
{
    private static readonly TimeSpan pollingInterval = TimeSpan.FromMilliseconds(500);

    static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        var folderPath = @"c:\temp";
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

        watcher.EnableLogging = false;
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
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            });
}

