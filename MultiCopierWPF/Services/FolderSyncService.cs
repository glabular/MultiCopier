using Microsoft.Extensions.Logging;
using MultiCopierWPF.Interfaces;
using MultiCopierWPF.Models;
using MultiCopierWPF.Utilities;
using System.IO;

namespace MultiCopierWPF.Services;

public class FolderSyncService : IFolderSyncService
{
    private readonly ILogger<FolderSyncService> _logger;

    public FolderSyncService(ILogger<FolderSyncService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Recursively synchronizes a source directory with its corresponding backup directory.
    /// </summary>
    /// <param name="source">The source directory (master folder).</param>
    /// <param name="target">The target directory (backup location).</param>
    public void Mirror(DirectoryInfo source, DirectoryInfo target, SyncContext context)
    {
        try
        {
            _logger.LogInformation("Starting mirror from {Source} to {Target}", source.FullName, target.FullName);

            Directory.CreateDirectory(target.FullName);

            // Account for FAT32's 2-second timestamp granularity to avoid unnecessary copies
            var timeToleranceSeconds = FileSystemHelper.IsFat32(target) ? 2 : 0;

            // Get cached files & directories once
            var sourceFiles = source.GetFiles();
            var targetFiles = target.GetFiles();

            var sourceDirs = source.GetDirectories();
            var targetDirs = target.GetDirectories().ToDictionary(d => d.Name);

            SyncFiles(sourceFiles, targetFiles, target.FullName, timeToleranceSeconds, context);
            RemoveDeletedFiles(sourceFiles, targetFiles, target.FullName, context);

            SyncDirectories(sourceDirs, targetDirs, target, context);
            RemoveDeletedDirectories(sourceDirs, [.. targetDirs.Values], target.FullName, context);

            _logger.LogInformation("Completed mirror from {Source} to {Target}", source.FullName, target.FullName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during mirror from {Source} to {Target}", source.FullName, target.FullName);
            throw;
        }
    }

    private void SyncFiles(FileInfo[] sourceFiles, FileInfo[] targetFiles, string targetPath, int timeToleranceSeconds, SyncContext context)
    {
        var targetFileMap = targetFiles.ToDictionary(f => f.Name);

        foreach (var sFile in sourceFiles)
        {
            if (!targetFileMap.TryGetValue(sFile.Name, out var tFile))
            {
                // New file - copy it
                _logger.LogInformation("Copying new file {FileName} to {TargetDir}", sFile.Name, targetPath);
                CopyAndPreserveTimestamp(sFile, Path.Combine(targetPath, sFile.Name));
                context.FilesCopied++;
            }
            else if (ShouldCopy(sFile, tFile, timeToleranceSeconds))
            {
                // Updated file - overwrite
                _logger.LogInformation("Updating file {FileName} in {TargetDir}", sFile.Name, targetPath);
                CopyAndPreserveTimestamp(sFile, tFile.FullName, overwrite: true);
                context.FilesUpdated++;
            }
            else
            {
                _logger.LogDebug("File {FileName} is up to date in {TargetDir}", sFile.Name, targetPath);
            }
        }
    }

    private void RemoveDeletedFiles(FileInfo[] sourceFiles, FileInfo[] targetFiles, string targetPath, SyncContext context)
    {
        var sourceFileNames = new HashSet<string>(sourceFiles.Select(f => f.Name));

        foreach (var tFile in targetFiles)
        {
            if (!sourceFileNames.Contains(tFile.Name))
            {
                _logger.LogInformation("Deleting file {FileName} from {TargetDir}", tFile.Name, targetPath);
                tFile.Delete();
                context.FilesDeleted++;
            }
        }
    }

    private void SyncDirectories(DirectoryInfo[] sourceDirs, Dictionary<string, DirectoryInfo> targetDirMap, DirectoryInfo targetParent, SyncContext context)
    {
        foreach (var sDir in sourceDirs)
        {
            if (!targetDirMap.TryGetValue(sDir.Name, out var tDir))
            {
                _logger.LogInformation("Creating directory {DirName} in {TargetDir}", sDir.Name, targetParent.FullName);
                tDir = targetParent.CreateSubdirectory(sDir.Name);
                context.DirectoriesCreated++;
            }

            Mirror(sDir, tDir, context); // Recursive call
        }
    }

    private void RemoveDeletedDirectories(DirectoryInfo[] sourceDirs, DirectoryInfo[] targetDirs, string targetPath, SyncContext context)
    {
        var sourceDirNames = new HashSet<string>(sourceDirs.Select(d => d.Name));

        foreach (var tDir in targetDirs)
        {
            if (!sourceDirNames.Contains(tDir.Name))
            {
                _logger.LogInformation("Deleting directory {DirName} from {TargetDir}", tDir.Name, targetPath);
                tDir.Delete(true);
                context.DirectoriesDeleted++;
            }
        }
    }

    /// <summary>
    /// Determines whether the target file needs to be updated by comparing the source and target files.
    /// Returns true if the files differ in size or if their last write timestamps differ by more than the allowed tolerance.
    /// This helps avoid unnecessary copying due to minor timestamp differences, especially on FAT32 file systems.
    /// </summary>
    /// <param name="timeToleranceSeconds">Allowed difference in seconds for timestamps to consider files identical.</param>
    /// <returns>True if the file should be copied (updated); otherwise, false.</returns>
    private bool ShouldCopy(FileInfo source, FileInfo target, int timeToleranceSeconds)
    {
        var timeDifference = Math.Abs((source.LastWriteTimeUtc - target.LastWriteTimeUtc).TotalSeconds);
        var shouldCopy = timeDifference > timeToleranceSeconds || source.Length != target.Length;

        _logger.LogTrace("Comparing files {SourceFile} and {TargetFile}: shouldCopy={ShouldCopy}",
            source.FullName, target.FullName, shouldCopy);

        return shouldCopy;
    }

    /// <summary>
    /// Copies the source file to the specified destination path.
    /// If the destination file exists and overwrite is true, it will be replaced.
    /// After copying, sets the destination file's last write timestamp to match the source file's timestamp,
    /// ensuring consistent timestamps to prevent unnecessary future copies.
    /// </summary>
    /// <param name="source">The source file info to copy from.</param>
    /// <param name="destinationPath">The full path where the file should be copied to.</param>
    /// <param name="overwrite">Whether to overwrite the destination file if it already exists. Defaults to false.</param>
    private void CopyAndPreserveTimestamp(FileInfo source, string destinationPath, bool overwrite = false)
    {
        _logger.LogDebug("Copying file {SourceFile} to {DestinationPath} (overwrite={Overwrite})",
            source.FullName, destinationPath, overwrite);

        source.CopyTo(destinationPath, overwrite);
        File.SetLastWriteTimeUtc(destinationPath, source.LastWriteTimeUtc);
    }
}
