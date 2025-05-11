namespace MultiCopierWPF.Models;

/// <summary>
/// Represents the result of a shallow comparison between a master and backup folder.
/// Contains the number of mismatched files and directories, and a helper property to indicate if any mismatch was found.
/// </summary>
public class FolderComparisonResult
{
    public bool HasMismatch => FileMismatchCount > 0 || DirectoryMismatchCount > 0;

    public int FileMismatchCount { get; init; }

    public int DirectoryMismatchCount { get; init; }
}
