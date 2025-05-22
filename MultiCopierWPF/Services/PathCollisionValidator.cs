using Microsoft.Extensions.Logging;
using System.IO;

namespace MultiCopierWPF.Services;

/// <summary>
/// Provides validation for folder path configurations to prevent conflicts and unsafe copying behaviors.
/// </summary>
public static class PathCollisionValidator
{
    /// <summary>
    /// Validates that the master folder and backup folders do not conflict (e.g. duplicates, overlaps, nesting).
    /// </summary>
    /// <param name="masterPath">The full path to the master folder.</param>
    /// <param name="backupPaths">A collection of backup folder paths to validate against the master.</param>
    /// <param name="errorMessage">An error message describing the validation failure, if any.</param>
    /// <returns><c>true</c> if validation passes; otherwise, <c>false</c> with an appropriate error message.</returns>
    public static bool TryValidate(string masterPath, IEnumerable<string> backupPaths, out string? errorMessage)
    {
        errorMessage = null;

        var normalizedMaster = Path.GetFullPath(masterPath).TrimEnd(Path.DirectorySeparatorChar);

        var normalizedBackups = backupPaths
            .Select(p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar))
            .ToList();

        // 0. Duplicate backup paths
        var distinctPaths = new HashSet<string>(normalizedBackups, StringComparer.OrdinalIgnoreCase);
        if (distinctPaths.Count < normalizedBackups.Count)
        {
            errorMessage = "Duplicate backup locations detected. Each backup must have a unique path.";
            return false;
        }

        // 1. Master == any backup
        if (normalizedBackups.Any(p => string.Equals(p, normalizedMaster, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "One of the backup locations is the same as the master folder.";
            return false;
        }

        // 2. Backup inside master
        if (normalizedBackups.Any(p => p.StartsWith(normalizedMaster + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "One of the backup locations is inside the master folder. This would cause recursive copying.";
            return false;
        }

        // 3. Backup inside another backup
        for (int i = 0; i < normalizedBackups.Count; i++)
        {
            for (int j = i + 1; j < normalizedBackups.Count; j++)
            {
                string a = normalizedBackups[i];
                string b = normalizedBackups[j];

                if (a.StartsWith(b + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    b.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    errorMessage = "One of the backup locations is nested within another backup location.";
                    return false;
                }
            }
        }

        return true;
    }
}
