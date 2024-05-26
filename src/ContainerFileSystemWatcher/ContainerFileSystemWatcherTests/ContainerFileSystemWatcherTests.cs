using ContainerFileSystemWatcher;
using Microsoft.Extensions.Logging;
using Moq;

namespace ContainerFileSystemWatcherTests;

public class ContainerFileSystemWatcherTests
{
    [Fact]
    public async Task ShouldDetectFileCreation()
    {
        // Arrange
        var testFileSystemShim = new TestFileSystemShim();
        var mockLogger = new Mock<ILogger<ContainerFileWatcher>>();
        var tempDir = "/mock/path";

        testFileSystemShim.AddDirectory(tempDir);

        var watcher = new ContainerFileWatcher(testFileSystemShim, mockLogger.Object);
        watcher.AddWatch(tempDir, TimeSpan.FromMilliseconds(100));

        var fileCreated = false;
        watcher.OnFileChanged += (changeType, filePath) =>
        {
            if (changeType == ChangeType.Created)
            {
                fileCreated = true;
                Console.WriteLine($"File created event detected for: {filePath}");
            }
        };

        // Act
        await Task.Delay(200); // Wait for the first polling interval
        testFileSystemShim.AddFile(tempDir, "testfile.txt");

        await Task.Delay(200); // Wait for another polling interval

        // Assert
        Assert.True(fileCreated);
    }

    [Fact]
    public async Task ShouldDetectFileDeletion()
    {
        // Arrange
        var testFileSystemShim = new TestFileSystemShim();
        var mockLogger = new Mock<ILogger<ContainerFileWatcher>>();
        var tempDir = "/mock/path";
        var testFilePath = Path.Combine(tempDir, "testfile.txt");

        testFileSystemShim.AddDirectory(tempDir);
        testFileSystemShim.AddFile(tempDir, "testfile.txt");

        var watcher = new ContainerFileWatcher(testFileSystemShim, mockLogger.Object);
        watcher.AddWatch(tempDir, TimeSpan.FromMilliseconds(100));

        var fileDeleted = false;
        watcher.OnFileChanged += (changeType, filePath) =>
        {
            if (changeType == ChangeType.Deleted)
            {
                fileDeleted = true;
                Console.WriteLine($"File deleted event detected for: {filePath}");
            }
        };

        // Act
        await Task.Delay(200); // Wait for the first polling interval
        testFileSystemShim.RemoveFile(tempDir, "testfile.txt");

        await Task.Delay(200); // Wait for another polling interval

        // Assert
        Assert.True(fileDeleted);
    }

    [Fact]
    public async Task ShouldDetectFileModification()
    {
        // Arrange
        var testFileSystemShim = new TestFileSystemShim();
        var mockLogger = new Mock<ILogger<ContainerFileWatcher>>();
        var tempDir = "/mock/path";
        var testFilePath = Path.Combine(tempDir, "testfile.txt");

        testFileSystemShim.AddDirectory(tempDir);
        testFileSystemShim.AddFile(tempDir, "testfile.txt");

        var watcher = new ContainerFileWatcher(testFileSystemShim, mockLogger.Object);
        watcher.AddWatch(tempDir, TimeSpan.FromMilliseconds(100));

        var fileModified = false;
        watcher.OnFileChanged += (changeType, filePath) =>
        {
            if (changeType == ChangeType.Modified)
            {
                fileModified = true;
                Console.WriteLine($"File modified event detected for: {filePath}");
            }
        };

        // Act
        await Task.Delay(200); // Wait for the first polling interval
        testFileSystemShim.ModifyFile(tempDir, "testfile.txt");

        await Task.Delay(200); // Wait for another polling interval

        // Assert
        Assert.True(fileModified);
    }
}
