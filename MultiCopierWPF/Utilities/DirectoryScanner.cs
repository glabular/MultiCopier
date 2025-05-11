using MultiCopierWPF.Models;
using System.IO;

namespace MultiCopierWPF.Utilities;

public static class DirectoryScanner
{
    /// <summary>
    /// Asynchronously scans all files in the master folder (including subfolders)
    /// and returns a list of file entries with basic information.
    /// </summary>
    /// <param name="masterFolderPath">The full path to the master folder.</param>
    /// <returns>
    /// A list of <see cref="ScannedFileEntry"/> objects, each representing a file
    /// with its full path, name, size, and last modified time.
    /// </returns>
    public static Task<List<ScannedFileEntry>> GetMasterFolderFilesAsync(string masterFolderPath)
    {
        return Task.Run(() =>
        {
            var result = new List<ScannedFileEntry>();
            var dirInfo = new DirectoryInfo(masterFolderPath);

            foreach (var file in dirInfo.GetFiles("*", SearchOption.AllDirectories))
            {
                result.Add(new ScannedFileEntry(
                    file.FullName,
                    file.Name,
                    file.Length,
                    file.LastWriteTime
                ));
            }

            return result;
        });
    }
}
