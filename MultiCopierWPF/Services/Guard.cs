using System.IO;

namespace MultiCopierWPF.Services;

public static class Guard
{
    /// <summary>
    /// Ensures the given path is not null, not empty, and points to an existing directory.
    /// </summary>
    /// <param name="path">The folder path to validate.</param>
    /// <param name="paramName">Optional parameter name for better exception messages.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="DirectoryNotFoundException"></exception>
    public static void AgainstInvalidPath(string? path, string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is null, empty, or whitespace.", paramName);
        }

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {path}");
        }
    }
}
