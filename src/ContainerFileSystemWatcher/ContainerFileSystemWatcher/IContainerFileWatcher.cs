
namespace ContainerFileSystemWatcher
{
    public interface IContainerFileWatcher
    {
        event Action<ChangeType, string>? OnFileChanged;
        void AddWatch(string path, TimeSpan pollingInterval);
        void RemoveWatch(string path);
    }
}