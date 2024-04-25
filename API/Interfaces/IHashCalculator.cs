namespace API.Interfaces;

public interface IHashCalculator
{
    string GetSHA512(string path);
}
