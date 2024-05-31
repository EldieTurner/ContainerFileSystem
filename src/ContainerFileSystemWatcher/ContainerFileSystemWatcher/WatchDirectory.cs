using System.Collections.Generic;
using System;

namespace ContainerFileSystemWatcher
{
    /// <summary>
    /// Represents a directory to be watched for changes.
    /// </summary>
    internal class WatchDirectory
    {
        /// <summary>
        /// The path of the directory to watch.
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// The interval at which to poll the directory for changes.
        /// </summary>
        public TimeSpan PollingInterval { get; }
        /// <summary>
        /// Dictionary of file paths and their last write times.
        /// </summary>
        public Dictionary<string, DateTime> Snapshot { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WatchDirectory"/> class.
        /// </summary>
        /// <param name="path">The path of the directory to watch.</param>
        /// <param name="pollingInterval">The interval at which to poll the directory for changes.</param>
        /// <param name="snapshot">The initial snapshot of the directory.</param>
        public WatchDirectory(string path, TimeSpan pollingInterval, Dictionary<string, DateTime> snapshot)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            PollingInterval = pollingInterval;
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        }
    }
}
