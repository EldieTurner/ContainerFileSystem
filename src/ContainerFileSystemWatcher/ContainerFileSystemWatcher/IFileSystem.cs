namespace ContainerFileSystemWatcher;

public interface IFileSystem
{
    bool DirectoryExists(string path);

    Dictionary<string, DateTime> GetDirectorySnapshot(string path);

    DateTime GetLastWriteTime(string path);

    IEnumerable<string> GetFiles(string path);
}