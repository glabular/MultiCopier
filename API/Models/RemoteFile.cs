namespace API.Models;

public class RemoteFile
{
    public int ID { get; set; }

    public string OriginalFileName { get; set; }

    public string OriginalFilePath { get; set; }

    public string? EncryptedFilePath { get; set; }

    public string EncryptedFileName { get; set; }

    public byte[] EncryptionKey { get; set; }

    public byte[] IV { get; set; }

    public string SHA512 { get; set; }

    public DateTime UploadTimestamp { get; set; }
}
