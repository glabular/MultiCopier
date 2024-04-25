using API.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace API.Services;

public class HashCalculator : IHashCalculator
{
    public string GetSHA512(string path)
    {
        try
        {
            using var mySHA512 = SHA512.Create();
            using FileStream fileStream = File.OpenRead(path);
            fileStream.Position = 0;
            var hashValue = mySHA512.ComputeHash(fileStream);

            return ByteArrayToString(hashValue);
        }
        catch (IOException e)
        {
            throw new IOException($"Error reading file: {path}", e);
        }
        catch (UnauthorizedAccessException e)
        {
            throw new UnauthorizedAccessException($"Access denied to file: {path}", e);
        }

        static string ByteArrayToString(byte[] array)
        {
            StringBuilder sb = new();

            for (int i = 0; i < array.Length; i++)
            {
                sb.Append($"{array[i]:X2}");
            }

            return sb.ToString();
        }
    }
}
