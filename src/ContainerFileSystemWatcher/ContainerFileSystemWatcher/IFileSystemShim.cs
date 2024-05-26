using System;
using System.Collections.Generic;

namespace ContainerFileSystemWatcher
{
    public interface IFileSystemShim
    {
        bool DirectoryExists(string path);

        Dictionary<string, DateTime> GetDirectorySnapshot(string path);

        DateTime GetLastWriteTime(string path);

        IEnumerable<string> GetFiles(string path);
    }
}