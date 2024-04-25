namespace API.Requests;

public class EncryptFileRequest
{
    public byte[] EncryptionKey { get; set; }

    public byte[] IV { get; set; }

    public string InputFilePath { get; set; }

    public string EncryptedFilePath { get; set; }

    public string EncryptedName { get; set; }
}
