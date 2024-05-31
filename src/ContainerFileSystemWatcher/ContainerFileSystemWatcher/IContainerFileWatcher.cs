
using System;

namespace ContainerFileSystemWatcher
{
    /// <summary>
    /// Provides functionality to watch directories for file changes.
    /// </summary>
    public interface IContainerFileWatcher
    {
        /// <summary>
        /// Gets or sets a value indicating whether logging is enabled.
        /// </summary>
        bool EnableLogging { get; set; }

        /// <summary>
        /// Occurs when a file in a watched directory changes.
        /// </summary>
        event Action<ChangeType, string> OnFileChanged;

        /// <summary>
        /// Adds a directory to the watch list with the specified polling interval.
        /// </summary>
        /// <param name="path">The path of the directory to watch.</param>
        /// <param name="pollingInterval">The interval at which to poll the directory for changes.</param>
        void AddWatch(string path, TimeSpan pollingInterval);

        /// <summary>
        /// Removes a directory from the watch list.
        /// </summary>
        /// <param name="path">The path of the directory to remove from the watch list.</param>
        void RemoveWatch(string path);
    }
}