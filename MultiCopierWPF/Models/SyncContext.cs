namespace MultiCopierWPF.Models;

/// <summary>
/// Stores information about what happened during a folder sync operation.
/// This includes how many files were copied, updated, or deleted,
/// and how many folders were created or deleted.
/// </summary>
public class SyncContext
{
    /// <summary>
    /// The number of new files that were copied from the source to the target folder.
    /// </summary>
    public int FilesCopied { get; set; }

    /// <summary>
    /// The number of files in the target folder that were replaced with updated versions.
    /// </summary>
    public int FilesUpdated { get; set; }

    /// <summary>
    /// The number of files that were deleted from the target folder because they don't exist in the source.
    /// </summary>
    public int FilesDeleted { get; set; }

    /// <summary>
    /// The number of new folders that were created in the target folder to match the source.
    /// </summary>
    public int DirectoriesCreated { get; set; }

    /// <summary>
    /// The number of folders that were deleted from the target folder because they don't exist in the source.
    /// </summary>
    public int DirectoriesDeleted { get; set; }
}
