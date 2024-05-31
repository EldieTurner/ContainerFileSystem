using System;
using System.Collections.Generic;

namespace ContainerFileSystemWatcher
{
    /// <summary>
    /// FileSystemShim is a wrapper around the System.IO classes to allow for easier testing.
    /// </summary>
    public interface IFileSystemShim
    {
        /// <summary>
        /// Checks if the specified directory exists.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>True if the directory exists, otherwise false.</returns>
        bool DirectoryExists(string path);

        /// <summary>
        /// Gets a snapshot of the directory, listing all files with their last write times.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>A dictionary where the keys are file paths and the values are the last write times of the files.</returns>
        Dictionary<string, DateTime> GetDirectorySnapshot(string path);

        /// <summary>
        /// Gets the last write time of the specified file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The last write time of the file.</returns>
        DateTime GetLastWriteTime(string path);

        /// <summary>
        /// Gets all the files in the specified directory.
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <returns>An enumerable collection of file paths.</returns>
        IEnumerable<string> GetFiles(string path);
    }
}