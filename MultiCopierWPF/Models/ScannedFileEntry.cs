namespace MultiCopierWPF.Models;

/// <summary>
/// Represents basic metadata for a file discovered during a directory scan.
/// </summary>
public class ScannedFileEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScannedFileEntry"/> class, representing a file entry with its
    /// associated metadata.
    /// </summary>
    /// <param name="fullPath">The full path of the file, including the directory and file name.</param>
    /// <param name="fileName">The name of the file, including its extension.</param>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <param name="lastModified">The date and time when the file was last modified.</param>
    public ScannedFileEntry(string fullPath, string fileName, long fileSize, DateTime lastModified)
    {
        FullPath = fullPath;
        FileName = fileName;
        FileSize = fileSize;
        LastModified = lastModified;
    }

    /// <summary>
    /// Gets or sets the full path to the file or directory.
    /// </summary>
    public string FullPath { get; set; }

    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the object was last modified.
    /// </summary>
    public DateTime LastModified { get; set; }
}
