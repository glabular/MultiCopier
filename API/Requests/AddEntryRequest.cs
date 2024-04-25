using API.Models;

namespace API.Requests;

public class AddEntryRequest
{
    public RemoteFile RemoteFile { get; set; }

    public string TableName { get; set; }
}
