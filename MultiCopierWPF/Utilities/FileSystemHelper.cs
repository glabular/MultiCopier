using MultiCopierWPF.Exceptions;
using System.IO;

namespace MultiCopierWPF.Utilities;

public static class FileSystemHelper
{
    /// <summary>
    /// Determines whether the drive of the specified path uses the FAT32 file system.
    /// </summary>
    /// <param name="path">The full path to a file or directory.</param>
    /// <returns><c>true</c> if the drive format is FAT32; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentException">Thrown when the path is invalid or not rooted.</exception>
    public static bool IsFat32(string path)
    {
        return GetDriveFormat(path).Equals("FAT32", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the drive of the specified directory uses the FAT32 file system.
    /// </summary>
    /// <param name="dir">The <see cref="DirectoryInfo"/> representing the directory.</param>
    /// <returns><c>true</c> if the drive format is FAT32; otherwise, <c>false</c>.</returns>
    public static bool IsFat32(DirectoryInfo dir)
    {
        return IsFat32(dir.FullName);
    }

    /// <summary>
    /// Ensures that the specified backup folder resides on a drive with enough free space 
    /// to store the entire contents of the source folder.
    /// </summary>
    /// <param name="sourceFolder">The folder whose contents need to be backed up.</param>
    /// <param name="backupPath">The destination path where the backup will be stored.</param>
    /// <exception cref="NotEnoughDiskSpaceException">
    /// Thrown if the target drive has less free space than required to back up the source folder.
    /// </exception>
    public static void EnsureEnoughDiskSpace(DirectoryInfo sourceFolder, string backupPath)
    {
        var requiredSpace = CalculateDirectorySize(sourceFolder);
        var drive = new DriveInfo(Path.GetPathRoot(backupPath)!);

        if (drive.AvailableFreeSpace < requiredSpace)
        {
            throw new NotEnoughDiskSpaceException($"Not enough free space on drive '{drive.Name}' for backup to '{backupPath}'. " +
                                    $"Required: {requiredSpace / (1024 * 1024)} MB, Available: {drive.AvailableFreeSpace / (1024 * 1024)} MB.");
        }
    }

    /// <summary>
    /// Gets the file system format of the drive where the specified path is located.
    /// </summary>
    /// <param name="path">A fully qualified path.</param>
    /// <returns>The file system format as a string (e.g., "NTFS", "FAT32").</returns>
    /// <exception cref="ArgumentException">Thrown when the path is null, empty, or not a valid root path.</exception>
    private static string GetDriveFormat(string path)
    {
        var root = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(root))
        {
            throw new ArgumentException("Path must be a valid drive path.", nameof(path));
        }

        return new DriveInfo(root).DriveFormat;
    }

    /// <summary>
    /// Calculates the total size of all files in a directory, including files in all subdirectories.
    /// </summary>
    /// <param name="directory">The root directory to measure.</param>
    /// <returns>The total size of all files in bytes.</returns>
    private static long CalculateDirectorySize(DirectoryInfo directory)
    {
        long size = 0;

        foreach (var file in directory.GetFiles("*", SearchOption.AllDirectories))
        {
            size += file.Length;
        }

        return size;
    }
}
