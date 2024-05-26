using System.Collections.Generic;
using System;

namespace ContainerFileSystemWatcher
{
    internal class WatchDirectory
    {
        public string Path { get; }
        public TimeSpan PollingInterval { get; }
        public Dictionary<string, DateTime> Snapshot { get; set; }

        public WatchDirectory(string path, TimeSpan pollingInterval, Dictionary<string, DateTime> snapshot)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            PollingInterval = pollingInterval;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }
    }
}
