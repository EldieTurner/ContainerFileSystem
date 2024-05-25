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
        var mockFileSystem = new Mock<IFileSystem>();
        var mockLogger = new Mock<ILogger<ContainerFileWatcher>>();
        var tempDir = "/mock/path";

        mockFileSystem.Setup(fs => fs.DirectoryExists(tempDir)).Returns(true);
        mockFileSystem.SetupSequence(fs => fs.GetDirectorySnapshot(tempDir))
            .Returns(new Dictionary<string, DateTime>()) // Initial empty state
            .Returns(new Dictionary<string, DateTime> { { Path.Combine(tempDir, "testfile.txt"), DateTime.Now } }); // State after file creation

        var watcher = new ContainerFileWatcher(mockFileSystem.Object, mockLogger.Object);
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
        await Task.Delay(500); // Wait for the polling interval

        // Assert
        Assert.True(fileCreated);
    }

    [Fact]
    public async Task ShouldDetectFileDeletion()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockLogger = new Mock<ILogger<ContainerFileWatcher>>();
        var tempDir = "/mock/path";
        var testFilePath = Path.Combine(tempDir, "testfile.txt");

        mockFileSystem.Setup(fs => fs.DirectoryExists(tempDir)).Returns(true);
        mockFileSystem.SetupSequence(fs => fs.GetDirectorySnapshot(tempDir))
            .Returns(new Dictionary<string, DateTime> { { testFilePath, DateTime.Now } }) // Initial state with file
            .Returns(new Dictionary<string, DateTime>()); // State after file deletion

        var watcher = new ContainerFileWatcher(mockFileSystem.Object, mockLogger.Object);
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
        await Task.Delay(500); // Wait for the polling interval

        // Assert
        Assert.True(fileDeleted);
    }

    [Fact]
    public async Task ShouldDetectFileModification()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var mockLogger = new Mock<ILogger<ContainerFileWatcher>>();
        var tempDir = "/mock/path";
        var testFilePath = Path.Combine(tempDir, "testfile.txt");

        mockFileSystem.Setup(fs => fs.DirectoryExists(tempDir)).Returns(true);
        mockFileSystem.SetupSequence(fs => fs.GetDirectorySnapshot(tempDir))
            .Returns(new Dictionary<string, DateTime> { { testFilePath, DateTime.Now } }) // Initial state
            .Returns(new Dictionary<string, DateTime> { { testFilePath, DateTime.Now.AddMinutes(1) } }); // State after modification

        var watcher = new ContainerFileWatcher(mockFileSystem.Object, mockLogger.Object);
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
        await Task.Delay(500); // Wait for the polling interval

        // Assert
        Assert.True(fileModified);
    }
}
