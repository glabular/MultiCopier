using API.Interfaces;
using API.Services;
using System.Data.SQLite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IFileEncryptor, FileEncryptor>();
        builder.Services.AddSingleton<IHashCalculator, HashCalculator>();

        // TODO: Replace hardcoded values
        var databaseName = "Archive";
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var multiCopierFolderPath = Path.Combine(documentsPath, "MultiCopier");
        var databasePath = Path.Combine(multiCopierFolderPath, $"{databaseName}.db");
        var connectionString = $"Data Source={databasePath};Version=3;";
        
        CreateDatabaseWithDefaultTable(connectionString, multiCopierFolderPath, databasePath);

        builder.Services.AddSingleton<IDBContext>(new API.Services.SQLiteContext(connectionString));

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();
        app.Run();





        await InitializeDatabases();

        async Task InitializeDatabases()
        {
            var hashCalculator = app.Services.GetRequiredService<IHashCalculator>();

            hashCalculator.GetSHA512("");

            // TODO: 
            var localArchivePath = @"D:\Архив\";
            var SSDArchivePath = @"H:\Архив\";
        }

                
    }

    private static void CreateTable(string connectionString, string tableName)
    {

        var createTableQuery = $@"CREATE TABLE IF NOT EXISTS {tableName} (
                                    ID INTEGER PRIMARY KEY,
                                    OriginalFileName TEXT,
                                    OriginalFilePath TEXT,
                                    EncryptedFileName TEXT,
                                    EncryptionKey BLOB,
                                    IV BLOB,
                                    SHA512 TEXT,
                                    EncryptedFilePath TEXT,
                                    UploadTimestamp DATETIME)";

        using var connection = new SQLiteConnection(connectionString);

        try
        {
            connection.Open();
            using var command = new SQLiteCommand(createTableQuery, connection);
            command.ExecuteNonQuery();
        }
        catch (Exception)
        {

            throw;
        }
        
    }

    private static void CreateDatabaseWithDefaultTable(string connectionString, string multiCopierFolderPath, string databasePath)
    {
        try
        {
            // Create the directory if it does not exist
            if (!Directory.Exists(multiCopierFolderPath))
            {
                Directory.CreateDirectory(multiCopierFolderPath);
            }

            if (!File.Exists(databasePath))
            {
                using var connection = new SQLiteConnection(connectionString);
                connection.Open();

                // If the database file doesn't exist, create it
                var createTableQuery = $@"CREATE TABLE IF NOT EXISTS Archive (
                                    ID INTEGER PRIMARY KEY,
                                    OriginalFileName TEXT,
                                    OriginalFilePath TEXT,
                                    EncryptedFileName TEXT,
                                    EncryptionKey BLOB,
                                    IV BLOB,
                                    SHA512 TEXT,
                                    UploadTimestamp DATETIME)";

                try
                {                    
                    using var command = new SQLiteCommand(createTableQuery, connection);
                    command.ExecuteNonQuery();
                    Console.WriteLine("SQLite database file created successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }                
            }
            else
            {
                Console.WriteLine("SQLite database file already exists.");
            }
        }
        catch (Exception ex)
        {
            // Log errors
        }
    }
}