namespace MultiCopierWPF.Models;

public class ScannedFileEntry
{
    public ScannedFileEntry(string fullPath, string fileName, long fileSize, DateTime lastModified)
    {
        FullPath = fullPath;
        FileName = fileName;
        FileSize = fileSize;
        LastModified = lastModified;
    }

    public string FullPath { get; set; }

    public string FileName { get; set; }

    public long FileSize { get; set; }

    public DateTime LastModified { get; set; }
}
