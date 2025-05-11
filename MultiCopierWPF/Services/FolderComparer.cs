using MultiCopierWPF.Exceptions;
using MultiCopierWPF.Models;
using System.IO;

namespace MultiCopierWPF.Services;

public static class FolderComparer
{
    /// <summary>
    /// Compares the total number of files and directories between a master folder and a backup folder.
    ///
    /// The result is returned as a <see cref="FolderComparisonResult"/>, which includes mismatch counts and a flag indicating if a mismatch was found.
    /// </summary>
    /// <param name="masterPath">Full path to the master folder.</param>
    /// <param name="backupPath">Full path to the backup folder to compare against.</param>
    /// <returns>
    /// A <see cref="FolderComparisonResult"/> containing the file and directory mismatch counts.
    /// </returns>
    public static async Task<FolderComparisonResult> CompareCountAsync(string masterPath, string backupPath)
    {
        // Start file comparison task
        var fileCountTask = Task.Run(() =>
        {
            var masterFileCount = Directory.GetFiles(masterPath, "*", SearchOption.AllDirectories).Length;
            var backupFileCount = Directory.GetFiles(backupPath, "*", SearchOption.AllDirectories).Length;
            return (masterFileCount, backupFileCount);
        });

        // Start directory comparison task
        var dirCountTask = Task.Run(() =>
        {
            var masterDirCount = Directory.GetDirectories(masterPath, "*", SearchOption.AllDirectories).Length;
            var backupDirCount = Directory.GetDirectories(backupPath, "*", SearchOption.AllDirectories).Length;
            return (masterDirCount, backupDirCount);
        });

        // Await both results
        var fileResult = await fileCountTask;
        var dirResult = await dirCountTask;

        // Calculate mismatches
        var fileCountMismatch = Math.Abs(fileResult.masterFileCount - fileResult.backupFileCount);
        var dirCountMismatch = Math.Abs(dirResult.masterDirCount - dirResult.backupDirCount);

        return new FolderComparisonResult
        {
            FileMismatchCount = Math.Abs(fileResult.masterFileCount - fileResult.backupFileCount),
            DirectoryMismatchCount = Math.Abs(dirResult.masterDirCount - dirResult.backupDirCount)
        };
    }
}
