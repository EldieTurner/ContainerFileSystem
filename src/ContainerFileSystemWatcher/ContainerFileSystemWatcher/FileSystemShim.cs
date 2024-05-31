using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ContainerFileSystemWatcher
{ 
    /// <summary>
    /// FileSystemShim is a wrapper around the System.IO classes to allow for easier testing.
    /// </summary>
    public class FileSystemShim : IFileSystemShim
    {
        /// <summary>
        /// Checks if the specified directory exists.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>True if the directory exists, otherwise false.</returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Gets a snapshot of the directory, listing all files with their last write times.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>A dictionary where the keys are file paths and the values are the last write times of the files.</returns>
        public Dictionary<string, DateTime> GetDirectorySnapshot(string path)
        {
            return Directory.GetFiles(path).ToDictionary(file => file, file => File.GetLastWriteTime(file));
        }

        /// <summary>
        /// Gets the last write time of the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The last write time of the file.</returns>
        public DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(path);
        }

        /// <summary>
        /// Gets all the files in the specified directory.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>An enumerable collection of file paths.</returns>
        public IEnumerable<string> GetFiles(string path)
        {
            return Directory.GetFiles(path);
        }

    }
}
