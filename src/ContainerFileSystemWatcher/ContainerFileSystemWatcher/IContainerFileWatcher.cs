
using System;

namespace ContainerFileSystemWatcher
{
    public interface IContainerFileWatcher
    {
        bool EnableLogging { get; set; }
        event Action<ChangeType, string> OnFileChanged;
        void AddWatch(string path, TimeSpan pollingInterval);
        void RemoveWatch(string path);
    }
}