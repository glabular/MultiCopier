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
        _logger.LogInformation("Starting mirror from {Source} to {Target}", source.FullName, target.FullName);

        Directory.CreateDirectory(target.FullName);

        // Account for FAT32's 2-second timestamp granularity to avoid unnecessary copies
        var timeToleranceSeconds = FileSystemHelper.IsFat32(target) ? 2 : 0;

        SyncFiles(source, target, timeToleranceSeconds, context);
        RemoveDeletedFiles(source, target, context);

        SyncDirectories(source, target, context);
        RemoveDeletedDirectories(source, target, context);

        _logger.LogInformation("Completed mirror from {Source} to {Target}", source.FullName, target.FullName);
    }

    private void SyncFiles(DirectoryInfo source, DirectoryInfo target, int timeToleranceSeconds, SyncContext context)
    {
        var sourceFiles = source.GetFiles();
        var targetFiles = target.GetFiles();
        var targetFileMap = targetFiles.ToDictionary(f => f.Name);

        foreach (var sFile in sourceFiles)
        {
            if (!targetFileMap.TryGetValue(sFile.Name, out var tFile))
            {
                // New file - copy it
                _logger.LogInformation("Copying new file {FileName} to {TargetDir}", sFile.Name, target.FullName);
                CopyAndPreserveTimestamp(sFile, Path.Combine(target.FullName, sFile.Name));
                context.FilesCopied++;
            }
            else if (ShouldCopy(sFile, tFile, timeToleranceSeconds))
            {
                // Updated file - overwrite
                _logger.LogInformation("Updating file {FileName} in {TargetDir}", sFile.Name, target.FullName);
                CopyAndPreserveTimestamp(sFile, tFile.FullName, overwrite: true);
                context.FilesUpdated++;
            }
            else
            {
                _logger.LogDebug("File {FileName} is up to date in {TargetDir}", sFile.Name, target.FullName);
            }
        }
    }

    private void RemoveDeletedFiles(DirectoryInfo source, DirectoryInfo target, SyncContext context)
    {
        var sourceFileNames = new HashSet<string>(source.GetFiles().Select(f => f.Name));
        var targetFiles = target.GetFiles();

        foreach (var tFile in targetFiles)
        {
            if (!sourceFileNames.Contains(tFile.Name))
            {
                _logger.LogInformation("Deleting file {FileName} from {TargetDir}", tFile.Name, target.FullName);
                tFile.Delete();
            }
        }
    }

    private void SyncDirectories(DirectoryInfo source, DirectoryInfo target, SyncContext context)
    {
        var sourceDirs = source.GetDirectories();
        var targetDirs = target.GetDirectories().ToDictionary(d => d.Name);

        foreach (var sDir in sourceDirs)
        {
            if (!targetDirs.TryGetValue(sDir.Name, out var tDir))
            {
                _logger.LogInformation("Creating directory {DirName} in {TargetDir}", sDir.Name, target.FullName);
                tDir = target.CreateSubdirectory(sDir.Name);
                context.DirectoriesCreated++;
            }

            Mirror(sDir, tDir, context); // Recursive call
        }
    }

    private void RemoveDeletedDirectories(DirectoryInfo source, DirectoryInfo target, SyncContext context)
    {
        var sourceDirNames = new HashSet<string>(source.GetDirectories().Select(d => d.Name));
        var targetDirs = target.GetDirectories();

        foreach (var tDir in targetDirs)
        {
            if (!sourceDirNames.Contains(tDir.Name))
            {
                _logger.LogInformation("Deleting directory {DirName} from {TargetDir}", tDir.Name, target.FullName);
                tDir.Delete(true);
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
