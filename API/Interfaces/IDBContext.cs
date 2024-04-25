using API.Models;
using API.Requests;

namespace API.Interfaces;

public interface IDBContext
{
    Task InsertRemoteFile(RemoteFile remoteFile);

    Task<RemoteFile?> GetByEncryptedFileName(string encryptedFileName);

    Task<RemoteFile?> GetById(int id);

    Task<List<RemoteFile>> GetAll();

    Task DeleteRemoteFile(RemoteFile file);

    Task DeleteRemoteFile(string encryptedFileName);
}
