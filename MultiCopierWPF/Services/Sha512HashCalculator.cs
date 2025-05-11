using Microsoft.Extensions.Logging;
using MultiCopierWPF.Interfaces;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MultiCopierWPF.Services;

/// <summary>
/// Provides functionality to compute SHA-512 hashes for files.
/// </summary>
public class Sha512HashCalculator : IHashCalculator
{
    private const string NullPathMessage = "File path cannot be null or whitespace.";
    private readonly ILogger<Sha512HashCalculator> _logger;

    public Sha512HashCalculator(ILogger<Sha512HashCalculator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string ComputeHash(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogError(NullPathMessage);
            throw new ArgumentException(NullPathMessage, nameof(path));
        }

        try
        {
            using var mySHA512 = SHA512.Create();
            using var fileStream = File.OpenRead(path);
            fileStream.Position = 0;
            var hashValue = mySHA512.ComputeHash(fileStream);
            var hashString = ByteArrayToString(hashValue);
            _logger.LogInformation("Successfully computed SHA-512 hash for file '{FilePath}'.", path);

            return hashString;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "I/O exception while computing hash for file '{FilePath}'.", path);
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access exception while computing hash for file '{FilePath}'.", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected exception while computing hash for file '{FilePath}'.", path);
            throw;
        }
    }

    /// <summary>
    /// Converts a byte array to its hexadecimal string representation.
    /// </summary>
    /// <param name="array">The byte array to convert.</param>
    /// <returns>A hexadecimal string.</returns>
    private static string ByteArrayToString(byte[] array)
    {
        StringBuilder sb = new();

        for (int i = 0; i < array.Length; i++)
        {
            sb.Append($"{array[i]:X2}");
        }

        return sb.ToString();
    }
}
