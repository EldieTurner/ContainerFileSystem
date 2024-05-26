using ContainerFileSystemWatcher;

public class TestFileSystemShim : IFileSystemShim
{
    private readonly Dictionary<string, Dictionary<string, DateTime>> _directories;

    public TestFileSystemShim()
    {
        _directories = new Dictionary<string, Dictionary<string, DateTime>>();
    }

    public void AddDirectory(string path)
    {
        if (!_directories.ContainsKey(path))
        {
            _directories[path] = new Dictionary<string, DateTime>();
        }
    }

    public void AddFile(string directoryPath, string fileName)
    {
        if (!_directories.ContainsKey(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var filePath = Path.Combine(directoryPath, fileName);
        _directories[directoryPath][filePath] = DateTime.Now;
    }

    public void ModifyFile(string directoryPath, string fileName)
    {
        if (!_directories.ContainsKey(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var filePath = Path.Combine(directoryPath, fileName);
        if (_directories[directoryPath].ContainsKey(filePath))
        {
            _directories[directoryPath][filePath] = DateTime.Now.AddMinutes(1);
        }
        else
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
    }

    public void RemoveFile(string directoryPath, string fileName)
    {
        if (!_directories.ContainsKey(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }

        var filePath = Path.Combine(directoryPath, fileName);
        _directories[directoryPath].Remove(filePath);
    }

    public bool DirectoryExists(string path)
    {
        return _directories.ContainsKey(path);
    }

    public Dictionary<string, DateTime> GetDirectorySnapshot(string path)
    {
        if (!_directories.ContainsKey(path))
        {
            throw new DirectoryNotFoundException($"Directory not found: {path}");
        }

        return new Dictionary<string, DateTime>(_directories[path]);
    }

    public void Clear()
    {
        _directories.Clear();
    }

    public DateTime GetLastWriteTime(string path)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<string> GetFiles(string path)
    {
        throw new NotImplementedException();
    }
}
