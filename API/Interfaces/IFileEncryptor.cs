namespace API.Interfaces;

public interface IFileEncryptor
{
    Task EncryptFileAsync(string filePath, byte[] key, byte[] iv, string encryptedName, string encryptedFileOutputFolder);

    Task DecryptFileAsync(string encryptedFilePath, byte[] key, byte[] iv, string decryptedOutputFile);

    byte[] GenerateEncryptionKey(int size);
}
