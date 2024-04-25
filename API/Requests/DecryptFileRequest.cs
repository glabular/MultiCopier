namespace API.Requests;

public class DecryptFileRequest
{
    public string EncryptedFilePath { get; set; }
    public string DecryptedFilePath { get; set; }
    public byte[] EncryptionKey { get; set; }
    public byte[] IV { get; set; }
}
