namespace MultiCopierWPF.Interfaces;

public interface IHashCalculator
{
    /// <summary>
    /// Computes the hash value of the file located at the specified path.
    /// </summary>
    /// <param name="path">The full file path to compute the hash for.</param>
    /// <returns>
    /// A hexadecimal string representing the computed hash of the file.
    /// In case of failure, returns a descriptive error message.
    /// </returns>
    string ComputeHash(string path);
}
