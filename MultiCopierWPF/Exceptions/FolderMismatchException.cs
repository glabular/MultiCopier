namespace MultiCopierWPF.Exceptions;

/// <summary>
/// Exception thrown when there is a mismatch between the files and directories in the backup
/// folder compared to the master folder.
/// </summary>
public class FolderMismatchException : Exception
{
    public string BackupPath { get; }

    /// <summary>
    /// Gets the number of file mismatches between the master and backup folders.
    /// </summary>
    public int FileMismatchCount { get; }

    /// <summary>
    /// Gets the number of directory mismatches between the master and backup folders.
    /// </summary>
    public int DirectoryMismatchCount { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderMismatchException"/> class.
    /// </summary>
    /// <param name="backupPath">The path of the backup folder.</param>
    /// <param name="fileCount">The number of file mismatches.</param>
    /// <param name="dirCount">The number of directory mismatches.</param>
    public FolderMismatchException(string backupPath, int fileCount, int dirCount)
        : base($"Backup at '{backupPath}' has {fileCount} file and {dirCount} directory mismatches with master folder.")
    {
        BackupPath = backupPath;
        FileMismatchCount = fileCount;
        DirectoryMismatchCount = dirCount;
    }
}
