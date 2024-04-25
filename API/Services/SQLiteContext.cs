using API.Interfaces;
using API.Models;
using API.Requests;
using System.Data.SQLite;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace API.Services;

public class SQLiteContext : IDBContext
{
    private readonly string _connectionString;
    private SQLiteTransaction _transaction;

    public SQLiteContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<List<RemoteFile>> GetAll()
    {
        var allFiles = new List<RemoteFile>();

        using var connection = new SQLiteConnection(_connectionString);

        await connection.OpenAsync();

        var query = "SELECT * FROM Archive";

        using var command = new SQLiteCommand(query, connection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var remoteFile = new RemoteFile
            {
                ID = reader.GetInt32(0),
                OriginalFileName = reader.GetString(1),
                OriginalFilePath = reader.GetString(2),
                EncryptedFileName = reader.GetString(3),
                EncryptionKey = (byte[])reader.GetValue(4),
                IV = (byte[])reader.GetValue(5),
                SHA512 = reader.GetString(6),
                UploadTimestamp = reader.GetDateTime(7)
            };

            allFiles.Add(remoteFile);
        }

        return allFiles;
    }


    // Old one, working!
    public async Task InsertRemoteFile2(RemoteFile file)
    {
        var db1Path = @"C:\Users\Maxim\Documents\MultiCopier\Archive.db";
        var db2Path = @"D:\Архив\Archive.db";
        var db3Path = @"H:\Архив\Archive.db";

        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        // Omitting the EncryptedFilePath property to avoid the need for a separate DTO object.
        var addFileQuery = @"INSERT INTO Archive 
                        (OriginalFileName, OriginalFilePath, EncryptedFileName, EncryptionKey, IV, SHA512, UploadTimestamp)
                        VALUES 
                        (@OriginalFileName, @OriginalFilePath, @EncryptedFileName, @EncryptionKey, @IV, @SHA512, @UploadTimestamp)";

        using var command = new SQLiteCommand(addFileQuery, connection);

        // Using SQLiteParameter ensures that the parameters are properly formatted and escaped,
        // preventing potential SQL injection attacks and ensuring compatibility with SQLite's parameter handling.

        command.Parameters.Add(new SQLiteParameter("@OriginalFileName", file.OriginalFileName));
        command.Parameters.Add(new SQLiteParameter("@OriginalFilePath", file.OriginalFilePath));
        command.Parameters.Add(new SQLiteParameter("@EncryptedFileName", file.EncryptedFileName));
        command.Parameters.Add(new SQLiteParameter("@EncryptionKey", file.EncryptionKey));
        command.Parameters.Add(new SQLiteParameter("@IV", file.IV));
        command.Parameters.Add(new SQLiteParameter("@SHA512", file.SHA512));
        command.Parameters.Add(new SQLiteParameter("@UploadTimestamp", file.UploadTimestamp));

        await command.ExecuteNonQueryAsync();
    }

    public async Task<RemoteFile?> GetByEncryptedFileName(string encryptedFileName)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        var getFileQuery = $"SELECT * FROM Archive WHERE EncryptedFileName = @EncryptedFileName";

        using var command = new SQLiteCommand(getFileQuery, connection);
        command.Parameters.Add(new SQLiteParameter("@EncryptedFileName", encryptedFileName));

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var file = new RemoteFile
            {
                ID = reader.GetInt32(0),
                OriginalFileName = reader.GetString(1),
                OriginalFilePath = reader.GetString(2),
                EncryptedFileName = reader.GetString(3),
                EncryptionKey = (byte[])reader.GetValue(4),
                IV = (byte[])reader.GetValue(5),
                SHA512 = reader.GetString(6),
                UploadTimestamp = reader.GetDateTime(7)
            };

            return file;
        }
        else
        {
            return null;
        }
    }

    public async Task<RemoteFile?> GetById(int id)
    {
        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        var getFileQuery = $"SELECT * FROM Archive WHERE Id = @Id";

        using var command = new SQLiteCommand(getFileQuery, connection);
        command.Parameters.Add(new SQLiteParameter("@Id", id));

        using var reader = await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var file = new RemoteFile
            {
                ID = reader.GetInt32(0),
                OriginalFileName = reader.GetString(1),
                OriginalFilePath = reader.GetString(2),
                EncryptedFileName = reader.GetString(3),
                EncryptionKey = (byte[])reader.GetValue(4),
                IV = (byte[])reader.GetValue(5),
                SHA512 = reader.GetString(6),
                UploadTimestamp = reader.GetDateTime(7)
            };

            return file;
        }
        else
        {
            return null;
        }
    }

    public async Task DeleteRemoteFile(RemoteFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        await DeleteRemoteFile(file.EncryptedFileName);
    }

    public async Task DeleteRemoteFile(string encryptedFileName)
    {
        if (string.IsNullOrWhiteSpace(encryptedFileName))
        {
            throw new ArgumentNullException(nameof(encryptedFileName));
        }

        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();

        var deleteFileQuery = "DELETE FROM Archive WHERE EncryptedFileName = @EncryptedFileName";

        using var command = new SQLiteCommand(deleteFileQuery, connection);
        command.Parameters.Add(new SQLiteParameter("@EncryptedFileName", encryptedFileName));

        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Safely inserts a remote file entry into three different SQLite databases simultaneously, serving as backups.
    /// </summary>
    /// <param name="file">The remote file entry to be inserted into the databases.</param>
    /// <remarks>
    /// This method inserts a remote file entry into three SQLite backup databases simultaneously.
    /// It opens connections to the databases, starts a transaction for each connection, and inserts the entry into the 'Archive' table.
    /// SQLiteParameter objects are used to set parameters for the insert commands.
    /// The transaction is committed if all inserts succeed; otherwise, it is rolled back.
    /// </remarks>
    /// <seealso cref="RemoteFile"/>
    public async Task InsertRemoteFile(RemoteFile file)
    {
        var db1Path = @"C:\Users\Maxim\Documents\MultiCopier\Archive.db";
        var db2Path = @"D:\Архив\Archive.db";
        var db3Path = @"H:\Архив\Archive.db";

        using var connection = new SQLiteConnection(_connectionString);
        await connection.OpenAsync();


        // Begin transaction
        using var transaction = connection.BeginTransaction();

        try
        {
            // Attach the second and third databases
            var attachDbCommand = new SQLiteCommand($"ATTACH DATABASE '{db2Path}' AS Db2; ATTACH DATABASE '{db3Path}' AS Db3;", connection);
            await attachDbCommand.ExecuteNonQueryAsync();

            // Your insert query with references to attached databases
            var addFileQuery = @"
                    INSERT INTO Archive 
                    (OriginalFileName, OriginalFilePath, EncryptedFileName, EncryptionKey, IV, SHA512, UploadTimestamp)
                    VALUES 
                    (@OriginalFileName, @OriginalFilePath, @EncryptedFileName, @EncryptionKey, @IV, @SHA512, @UploadTimestamp);

                    INSERT INTO Db2.Archive 
                    (OriginalFileName, OriginalFilePath, EncryptedFileName, EncryptionKey, IV, SHA512, UploadTimestamp)
                    VALUES 
                    (@OriginalFileName, @OriginalFilePath, @EncryptedFileName, @EncryptionKey, @IV, @SHA512, @UploadTimestamp);

                    INSERT INTO Db3.Archive 
                    (OriginalFileName, OriginalFilePath, EncryptedFileName, EncryptionKey, IV, SHA512, UploadTimestamp)
                    VALUES 
                    (@OriginalFileName, @OriginalFilePath, @EncryptedFileName, @EncryptionKey, @IV, @SHA512, @UploadTimestamp);";

            using var command = new SQLiteCommand(addFileQuery, connection);

            // Using SQLiteParameter ensures that the parameters are properly formatted and escaped,
            // preventing potential SQL injection attacks and ensuring compatibility with SQLite's parameter handling.

            command.Parameters.Add(new SQLiteParameter("@OriginalFileName", file.OriginalFileName));
            command.Parameters.Add(new SQLiteParameter("@OriginalFilePath", file.OriginalFilePath));
            command.Parameters.Add(new SQLiteParameter("@EncryptedFileName", file.EncryptedFileName));
            command.Parameters.Add(new SQLiteParameter("@EncryptionKey", file.EncryptionKey));
            command.Parameters.Add(new SQLiteParameter("@IV", file.IV));
            command.Parameters.Add(new SQLiteParameter("@SHA512", file.SHA512));
            command.Parameters.Add(new SQLiteParameter("@UploadTimestamp", file.UploadTimestamp));

            // Execute the command
            await command.ExecuteNonQueryAsync();

            // Commit the transaction
            transaction.Commit();
        }
        catch (Exception ex)
        {
            // Rollback the transaction if an exception occurs
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// Displays information about the attached databases in the provided SQLite connection.
    /// </summary>
    /// <param name="connection">The SQLite connection to retrieve attached database information from.</param>
    public void ShowAttachedDatabases(SQLiteConnection connection)
    {
        try
        {        
            // Open the connection if it's not already open
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            // Execute a query to retrieve the list of attached databases
            var query = "PRAGMA database_list;";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            Console.WriteLine("Attached databases:");
            while (reader.Read())
            {
                string databaseName = reader.GetString(1);
                string databaseFile = reader.GetString(2);
                Console.WriteLine($"{databaseName}: {databaseFile}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while retrieving attached databases: {ex.Message}");
        }
    }
}
