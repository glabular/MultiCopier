namespace API.Requests;

public class CopyFileRequest
{
    public string SourceFilePath { get; set; }

    public string DestinationFolderPath { get; set; }
}
