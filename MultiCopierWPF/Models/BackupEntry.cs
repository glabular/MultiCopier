namespace MultiCopierWPF.Models;

public class BackupEntry
{
    /// <summary>
    /// Constructor for essential metadata.
    /// </summary>
    /// <param name="originalFileName">Name of the original file.</param>
    /// <param name="originalFilePath">Full path to the original file.</param>
    /// <param name="sha512">SHA-512 hash of the file.</param>
    /// <param name="fileSize">Size of the file in bytes.</param>
    public BackupEntry(string originalFileName, string originalFilePath, string sha512, long fileSize)
    {
        OriginalFileName = originalFileName;
        OriginalFilePath = originalFilePath;
        SHA512 = sha512;
        FileSize = fileSize;
        BackupTime = DateTime.UtcNow;
    }

    // EF Core needs this — give it safe dummy values to silence warnings
    private BackupEntry()
    {
        OriginalFileName = string.Empty;
        OriginalFilePath = string.Empty;
        SHA512 = string.Empty;
    }

    public int ID { get; set; }

    public string OriginalFileName { get; init; }

    public string OriginalFilePath { get; init; }

    public string SHA512 { get; set; }

    public long FileSize { get; set; }

    public string? EncryptedFileName { get; set; }

    public byte[]? EncryptionKey { get; set; }

    public byte[]? IV { get; set; }

    public string? EncryptedFilePath { get; set; }

    public DateTime BackupTime { get; set; }

    public DateTime LastModified { get; set; }        
}
