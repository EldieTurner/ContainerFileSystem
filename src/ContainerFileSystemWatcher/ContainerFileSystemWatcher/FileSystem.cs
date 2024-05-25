namespace ContainerFileSystemWatcher;

public class FileSystem : IFileSystem
{
    public bool DirectoryExists(string path)
    {
        return Directory.Exists(path);
    }

    public Dictionary<string, DateTime> GetDirectorySnapshot(string path)
    {
        return Directory.GetFiles(path).ToDictionary(file => file, file => File.GetLastWriteTime(file));
    }

    public DateTime GetLastWriteTime(string path)
    {
        return File.GetLastWriteTime(path);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return Directory.GetFiles(path);
    }
}
