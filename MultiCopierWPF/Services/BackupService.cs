using MultiCopierWPF.ViewModels;
using System.IO;

namespace MultiCopierWPF.Services;

public class BackupService
{
    public static async Task RunBackupAsync(string masterFolder, IEnumerable<string> backupFolders)
    {
        ValidateMasterFolder(masterFolder);
        ValidateBackupPaths(backupFolders);
        EnsureEnoughDiskSpace(masterFolder, backupFolders);

        var sourceDir = new DirectoryInfo(masterFolder);

        // Perform backup to each destination
        foreach (var backupFolder in backupFolders)
        {
            var targetDir = new DirectoryInfo(backupFolder);

            await Task.Run(() =>
            {
                CleanExtraFiles(sourceDir, targetDir);
                CopyAll(sourceDir, targetDir);
            });
        }

        // Future: Add encryption and log metadata to DB here
    }

    /// <summary>
    /// Recursively copies all files and directories from source to target.
    /// </summary>
    private static void CopyAll(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (var fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory.
        foreach (var diSourceSubDir in source.GetDirectories())
        {
            var nextTargetSubDir = target.CreateSubdirectory(diSourceSubDir.Name);
            CopyAll(diSourceSubDir, nextTargetSubDir);
        }
    }

    private static void EnsureEnoughDiskSpace(string sourceFolder, IEnumerable<string> backupFolders)
    {
        var requiredSpace = CalculateDirectorySize(new DirectoryInfo(sourceFolder));

        foreach (var backupPath in backupFolders)
        {
            var drive = new DriveInfo(Path.GetPathRoot(backupPath)!);
            if (drive.AvailableFreeSpace < requiredSpace)
            {
                throw new IOException($"Not enough free space on drive '{drive.Name}' for backup to '{backupPath}'. " +
                                      $"Required: {requiredSpace / (1024 * 1024)} MB, Available: {drive.AvailableFreeSpace / (1024 * 1024)} MB.");
            }
        }
    }

    private static long CalculateDirectorySize(DirectoryInfo directory)
    {
        long size = 0;

        // Add file sizes
        foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
        {
            size += file.Length;
        }

        return size;
    }

    // Remove files not present in source
    private static void CleanExtraFiles(DirectoryInfo source, DirectoryInfo target)
    {
        var sourceFiles = source.GetFiles().Select(f => f.Name).ToHashSet();
        foreach (var file in target.GetFiles())
        {
            if (!sourceFiles.Contains(file.Name))
            {
                file.Delete();
            }
        }

        // Recursively clean subdirectories
        var sourceDirs = source.GetDirectories().Select(d => d.Name).ToHashSet();
        foreach (var dir in target.GetDirectories())
        {
            if (sourceDirs.Contains(dir.Name))
            {
                var matchingSourceDir = source.GetDirectories().First(d => d.Name == dir.Name);
                CleanExtraFiles(matchingSourceDir, dir);
            }
            else
            {
                dir.Delete(true);
            }
        }
    }

    /// <summary>
    /// Validates that the master folder exists.
    /// </summary>
    /// <param name="masterPath">The path to the master folder.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the master folder does not exist.</exception>
    private static void ValidateMasterFolder(string masterPath)
    {
        if (string.IsNullOrWhiteSpace(masterPath))
        {
            throw new ArgumentException("Master folder path is null or whitespace.", nameof(masterPath));
        }

        if (!Directory.Exists(masterPath))
        {
            throw new DirectoryNotFoundException($"Master folder does not exist: {masterPath}");
        }
    }

    /// <summary>
    /// Validates that each backup path is non-null, non-empty, and exists.
    /// </summary>
    /// <param name="backupPaths">A list of paths to backup folders.</param>
    /// <exception cref="ArgumentNullException">Thrown if the collection is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if any path is null, empty, or whitespace.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown if any path does not exist.</exception>
    private static void ValidateBackupPaths(IEnumerable<string> backupPaths)
    {
        if (backupPaths is null)
        {
            throw new ArgumentNullException(nameof(backupPaths), "Backup paths collection is null.");
        }

        foreach (var path in backupPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("A backup path is empty or null.");
            }

            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"Backup folder does not exist: {path}");
            }
        }
    }
}