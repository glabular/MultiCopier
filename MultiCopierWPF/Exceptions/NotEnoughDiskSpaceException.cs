using System.IO;

namespace MultiCopierWPF.Exceptions;

public class NotEnoughDiskSpaceException : IOException
{
    public NotEnoughDiskSpaceException(string message) : base(message) { }
}
