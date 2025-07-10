using Microsoft.Extensions.Logging.Abstractions;
using MultiCopierWPF.Models;
using MultiCopierWPF.Services;

namespace MultiCopier.Tests;

public class FolderSyncServiceTests
{
    private const string BaseTempFolder = @"D:\Temp\MulticopierTESTS";
    private readonly NullLogger<FolderSyncService> _logger;

    public FolderSyncServiceTests()
    {
        _logger = NullLogger<FolderSyncService>.Instance;
    }

    /// <summary>
    /// This test checks that when the target folder is empty, 
    /// a new file from the source folder is copied into it.
    /// It verifies that the file appears in the target with the correct content,
    /// and that the operation is logged as a copy (not update or delete).
    /// </summary>
    [Fact]
    public void Mirror_CopiesNewFile_WhenTargetIsEmpty()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var fileName = "file.txt";
            var sourceFile = Path.Combine(sourcePath, fileName);
            File.WriteAllText(sourceFile, "Hello, world!");

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            var targetFile = Path.Combine(targetPath, fileName);
            Assert.True(File.Exists(targetFile), "File should be copied to target.");
            Assert.Equal("Hello, world!", File.ReadAllText(targetFile));
            Assert.Equal(1, context.FilesCopied);
            Assert.Equal(0, context.FilesUpdated);
            Assert.Equal(0, context.FilesDeleted);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test checks that if a file already exists in the target folder 
    /// but the one in the source folder has different content or a newer timestamp,
    /// the file in the target is updated to match the source.
    /// </summary>
    [Fact]
    public void Mirror_UpdatesFile_WhenSourceFileIsNewerOrDifferentSize()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var fileName = "file.txt";
            var sourceFile = Path.Combine(sourcePath, fileName);
            var targetFile = Path.Combine(targetPath, fileName);

            File.WriteAllText(sourceFile, "New content");
            File.WriteAllText(targetFile, "Old content");

            // Set target file timestamp older than source
            File.SetLastWriteTimeUtc(targetFile, DateTime.UtcNow.AddMinutes(-10));
            File.SetLastWriteTimeUtc(sourceFile, DateTime.UtcNow);

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            Assert.True(File.Exists(targetFile), "File should exist in target.");
            Assert.Equal("New content", File.ReadAllText(targetFile));
            Assert.Equal(0, context.FilesCopied);
            Assert.Equal(1, context.FilesUpdated);
            Assert.Equal(0, context.FilesDeleted);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test checks that if the target folder contains a file 
    /// that no longer exists in the source folder, 
    /// that file gets deleted from the target.
    /// </summary>
    [Fact]
    public void Mirror_DeletesFiles_WhenMissingInSource()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var targetFileName = "obsolete.txt";
            var targetFile = Path.Combine(targetPath, targetFileName);
            File.WriteAllText(targetFile, "Obsolete file");

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            Assert.False(File.Exists(targetFile), "Obsolete file should be deleted from target.");
            Assert.Equal(0, context.FilesCopied);
            Assert.Equal(0, context.FilesUpdated);
            Assert.Equal(1, context.FilesDeleted);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test checks that if the source folder contains a new subdirectory 
    /// (with files inside), the same subdirectory and its contents 
    /// are created in the target folder.
    /// </summary>
    [Fact]
    public void Mirror_CreatesNewDirectories_WhenMissingInTarget()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var newDirName = "NewSubDir";
            var newDirPath = Path.Combine(sourcePath, newDirName);
            Directory.CreateDirectory(newDirPath);

            var fileInsideNewDir = Path.Combine(newDirPath, "file.txt");
            File.WriteAllText(fileInsideNewDir, "Content");

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            var targetNewDir = Path.Combine(targetPath, newDirName);
            Assert.True(Directory.Exists(targetNewDir), "New directory should be created in target.");
            var targetFile = Path.Combine(targetNewDir, "file.txt");
            Assert.True(File.Exists(targetFile), "File inside new directory should be copied.");
            Assert.Equal(1, context.DirectoriesCreated);
            Assert.True(context.FilesCopied > 0);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test checks that if the target folder contains a subdirectory 
    /// that no longer exists in the source folder, 
    /// that directory and its contents are removed from the target.
    /// </summary>
    [Fact]
    public void Mirror_DeletesDirectories_WhenMissingInSource()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var obsoleteDirName = "ObsoleteDir";
            var obsoleteDirPath = Path.Combine(targetPath, obsoleteDirName);
            Directory.CreateDirectory(obsoleteDirPath);

            var fileInsideObsoleteDir = Path.Combine(obsoleteDirPath, "file.txt");
            File.WriteAllText(fileInsideObsoleteDir, "Obsolete content");

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            Assert.False(Directory.Exists(obsoleteDirPath), "Obsolete directory should be deleted from target.");
            Assert.Equal(0, context.DirectoriesCreated);
            Assert.Equal(1, context.DirectoriesDeleted);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test verifies that the mirroring process works 
    /// even for deeply nested directories and files.
    /// It ensures that subdirectories and their contents are mirrored correctly.
    /// </summary>
    [Fact]
    public void Mirror_RecursivelySyncsNestedDirectoriesAndFiles()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            // Create nested structure in source: /dir1/dir2/file.txt
            var dir1 = Path.Combine(sourcePath, "dir1");
            var dir2 = Path.Combine(dir1, "dir2");
            Directory.CreateDirectory(dir2);

            var nestedFile = Path.Combine(dir2, "file.txt");
            File.WriteAllText(nestedFile, "Nested content");

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            var targetNestedFile = Path.Combine(targetPath, "dir1", "dir2", "file.txt");
            Assert.True(File.Exists(targetNestedFile), "Nested file should be copied to target.");
            Assert.Equal("Nested content", File.ReadAllText(targetNestedFile));
            Assert.Equal(2, context.DirectoriesCreated); // dir1 and dir2
            Assert.Equal(1, context.FilesCopied);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test checks that if the target folder doesn't exist at all,
    /// the Mirror method will automatically create it before copying files from source.
    /// </summary>
    [Fact]
    public void Mirror_CreatesTargetDirectory_IfMissing()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = Path.Combine(Path.GetTempPath(), "NonExistentTarget_" + Guid.NewGuid());

        try
        {
            var fileName = "file.txt";
            var sourceFile = Path.Combine(sourcePath, fileName);
            File.WriteAllText(sourceFile, "Content");

            var sourceDir = new DirectoryInfo(sourcePath);
            var targetDir = new DirectoryInfo(targetPath);
            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(sourceDir, targetDir, context);

            // Assert
            Assert.True(Directory.Exists(targetPath), "Target directory should be created.");
            var targetFile = Path.Combine(targetPath, fileName);
            Assert.True(File.Exists(targetFile), "File should be copied to target.");
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test checks that if a file exists in both the source and target folders,
    /// and both files have the same content and timestamp,
    /// then nothing is copied or updated.
    /// </summary>
    [Fact]
    public void Mirror_DoesNotCopyFile_WhenSourceAndTargetAreIdentical()
    {
        // Arrange
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var fileName = "identical.txt";
            var sourceFile = Path.Combine(sourcePath, fileName);
            var targetFile = Path.Combine(targetPath, fileName);

            File.WriteAllText(sourceFile, "Same content");
            File.WriteAllText(targetFile, "Same content");

            // Set same timestamp
            var timestamp = DateTime.UtcNow;
            File.SetLastWriteTimeUtc(sourceFile, timestamp);
            File.SetLastWriteTimeUtc(targetFile, timestamp);

            var context = new SyncContext();
            var service = CreateService();

            // Act
            service.Mirror(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath), context);

            // Assert
            Assert.Equal(0, context.FilesCopied);
            Assert.Equal(0, context.FilesUpdated);
            Assert.Equal(0, context.FilesDeleted);
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test ensures that very long and deeply nested folder paths 
    /// (approaching or exceeding 260 characters) are handled correctly, 
    /// and files deep in the folder hierarchy are successfully copied.
    /// </summary>
    [Fact]
    public void Mirror_CopiesDeeplyNestedLongPath()
    {
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var deepFolder = sourcePath;
            for (int i = 0; i < 20; i++) // Should produce ~260+ character paths
            {
                deepFolder = Path.Combine(deepFolder, $"subfolder_{i:D2}");
            }

            Directory.CreateDirectory(deepFolder);
            var deepFile = Path.Combine(deepFolder, "deepfile.txt");
            File.WriteAllText(deepFile, "deep content");

            var context = new SyncContext();
            var service = CreateService();

            service.Mirror(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath), context);

            var expectedTargetFile = deepFile.Replace(sourcePath, targetPath);
            Assert.True(File.Exists(expectedTargetFile));
            Assert.Equal("deep content", File.ReadAllText(expectedTargetFile));
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test verifies that the mirror operation correctly handles files 
    /// with special characters and Unicode in their filenames.
    /// The test ensures such files are copied without errors.
    /// </summary>
    [Theory]
    [InlineData("тестовый_файл.txt")]
    [InlineData("file with spaces.txt")]
    [InlineData("file_#1!.txt")]
    [InlineData("文件.txt")]
    [InlineData("emoji_📁.txt")]
    public void Mirror_CopiesFile_WithVariousNames(string fileName)
    {
        var sourcePath = CreateTempDirectory();
        var targetPath = CreateTempDirectory();

        try
        {
            var sourceFile = Path.Combine(sourcePath, fileName);
            File.WriteAllText(sourceFile, "Test content");

            var context = new SyncContext();
            var service = CreateService();

            service.Mirror(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath), context);

            var expectedTargetFile = Path.Combine(targetPath, fileName);
            Assert.True(File.Exists(expectedTargetFile));
            Assert.Equal("Test content", File.ReadAllText(expectedTargetFile));
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    /// <summary>
    /// This test verifies that the mirror operation correctly copies files 
    /// to a FAT32-formatted USB drive. It ensures compatibility with removable 
    /// media and file systems that have limitations such as no support for 
    /// certain file attributes or long paths.
    /// </summary>
    [Fact]
    public void Mirror_CopiesFiles_OnFAT32UsbDrive()
    {
        var usbDrive = DriveInfo.GetDrives()
            .FirstOrDefault(d => 
                d.DriveType == DriveType.Removable 
                && d.IsReady 
                && d.DriveFormat == "FAT32");

        if (usbDrive == null)
        {
            // Skip test if no FAT32 USB drive found
            throw new Exception("No FAT32 USB drive found. Skipping test.");
            return;
        }

        var sourcePath = CreateTempDirectory();
        var targetPath = Path.Combine(usbDrive.RootDirectory.FullName, "FolderSyncTest_" + Guid.NewGuid());
        Directory.CreateDirectory(targetPath);

        try
        {
            // Setup source files and folders here
            File.WriteAllText(Path.Combine(sourcePath, "test.txt"), "Test content");

            var service = CreateService();
            var context = new SyncContext();

            service.Mirror(new DirectoryInfo(sourcePath), new DirectoryInfo(targetPath), context);

            var copiedFile = Path.Combine(targetPath, "test.txt");
            Assert.True(File.Exists(copiedFile));
            Assert.Equal("Test content", File.ReadAllText(copiedFile));
        }
        finally
        {
            CleanupDirectory(sourcePath);
            CleanupDirectory(targetPath);
        }
    }

    private FolderSyncService CreateService()
    {
        return new FolderSyncService(_logger);
    }

    // Helper: Creates a unique temporary directory for testing
    private string CreateTempDirectory()
    {
        //var path = Path.Combine(Path.GetTempPath(), "FolderSyncTest_" + Guid.NewGuid());
        var path = Path.Combine(BaseTempFolder, "FolderSyncTest_" + Guid.NewGuid());
        Directory.CreateDirectory(path);
        return path;
    }

    // Helper: Safely deletes a directory
    private void CleanupDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}