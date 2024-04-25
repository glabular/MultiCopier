using API.Interfaces;
using System.Security.Cryptography;

namespace API.Services;

public class FileEncryptor : IFileEncryptor
{
    private readonly ILogger<FileEncryptor> _logger;

    public FileEncryptor(ILogger<FileEncryptor> logger)
    {
        _logger = logger;
    }

    public async Task EncryptFileAsync(string filePath, byte[] key, byte[] iv, string encryptedName, string encryptedFilePath)
    {        
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var inputFile = File.OpenRead(filePath);
        using var outputFile = File.Create(encryptedFilePath);
        using var encryptor = aes.CreateEncryptor();
        using var cryptoStream = new CryptoStream(outputFile, encryptor, CryptoStreamMode.Write);

        await inputFile.CopyToAsync(cryptoStream);
        await cryptoStream.FlushAsync();
        await outputFile.FlushAsync();
    }

    public async Task DecryptFileAsync(string encryptedFilePath, byte[] key, byte[] iv, string decryptedOutputFile)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        Directory.CreateDirectory(Path.GetDirectoryName(decryptedOutputFile));

        using var inputFile = File.OpenRead(encryptedFilePath);
        using var outputFile = File.Create(decryptedOutputFile);
        using var decryptor = aes.CreateDecryptor();
        using var cryptoStream = new CryptoStream(inputFile, decryptor, CryptoStreamMode.Read);

        await cryptoStream.CopyToAsync(outputFile);
        await outputFile.FlushAsync();
    }

    public byte[] GenerateEncryptionKey(int size)
    {
        if (size <= 0)
        {
            throw new ArgumentException("Key size must be a positive integer.", nameof(size));
        }

        var key = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(key);

        return key;
    }
}
