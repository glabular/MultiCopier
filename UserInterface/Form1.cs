using API.Models;
using API.Requests;
using System.Text;
using Newtonsoft.Json;
using System.Text.Json;
using System.Diagnostics;
using System.Data.SQLite;

namespace UserInterface;

public partial class Form1 : Form
{
    private static string _destinationFolder = @"2019";
    private static readonly int _port = 7239; // TODO: Read the value from launchsettings file.
    private static readonly int _httpClientTimemout = 5400; // Seconds.
    private static readonly JsonSerializerOptions _deserializationOptions = new()
    {
        PropertyNameCaseInsensitive = true, // Ignore case when deserializing property names
    };

    public Form1()
    {
        InitializeComponent();
        InitializeTargetFolderCombobox();
        DisplayCurrentYearInCombobox();
    }

    public static string DestinationFolder
    {
        get { return _destinationFolder; }
        private set { _destinationFolder = value; }
    }

    public static string DiskOArchiveDestination
    {
        get => $@"O:\Архив\{DestinationFolder}";
    }

    public static string SSDArchiveDestination
    {
        get { return $@"H:\Архив\{DestinationFolder}"; }
    }

    public static string LocalArchiveDestination
    {
        get { return $@"D:\Архив\{DestinationFolder}"; }
    }

    public static string LocalArchiveRoot
    {
        get { return $@"D:\Архив\"; }
    }

    public static string BaseURL
    {
        get { return $"https://localhost:{_port}/api/"; }
    }

    public bool DeleteOriginalFileCheckbox { get; private set; }

    public static string TemporaryFolder
    {
        get { return @"D:\temp\MultiCopierTMP\"; }
    }

    public static string DiskOtemporaryFolder
    {
        get { return $"{TemporaryFolder}Disk-O"; }
    }

    /// <summary>
    /// Gets or sets the collection of temporary or disposable items.
    /// </summary>
    /// <remarks>
    /// This property represents a list of items designated as temporary or disposable within the workflow. 
    /// These items are pending deletion once they are no longer needed.
    /// </remarks>
    public List<string> Trash { get; }

    private void pannelAddFilesToEncrypt_DragEnter(object sender, DragEventArgs e)
    {
        pannelAddFilesToEncrypt.BackColor = Color.Green;
        e.Effect = DragDropEffects.Copy;
    }

    private void pannelAddFilesToEncrypt_DragLeave(object sender, EventArgs e)
    {
        pannelAddFilesToEncrypt.BackColor = Color.Gray;
        pannelAddFilesToEncrypt.Invalidate();
    }

    private async void pannelAddFilesToEncrypt_DragDrop(object sender, DragEventArgs e)
    {
        pannelAddFilesToEncrypt.BackColor = Color.Gray;
        var droppedFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

        // Copy dropped files to the local archive
        var copyingFilesToLocalArchive = CopyFiles(droppedFiles, LocalArchiveDestination);

        // Copy dropped files to the SSD archive
        var copyingFilesToSSDArchive = CopyFiles(droppedFiles, SSDArchiveDestination);

        var remoteFiles = await GetRemoteFiles(droppedFiles);

        await EncryptFiles(remoteFiles);

        
        if (await AddRemoteFilesToDatabase(remoteFiles) == 0)
        {
            // Success
        }
        else
        {
            return;
        }

        await CopyFilesToCloud(remoteFiles);

        await Task.WhenAll(copyingFilesToLocalArchive, copyingFilesToSSDArchive);

        var result = MessageBox.Show("Пожалуйста, убедитесь, что загрузка на удалённый сервер закончена. Временные файлы будут удалены.", "Подтверждение удаления временных файлов",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (result == DialogResult.Yes)
        {
            PerformCleanUp(remoteFiles);
            //MessageBox.Show($"Временная папка очищена!", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            // TODO: Add files for cleanup to a file on the disk and clean the next time app is launched.
            MessageBox.Show("Операция отменена. Временная папка не очищена.", "Отменено", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// Cleans up temporary files associated with the provided list.
    /// </summary>
    /// <param name="remoteFiles"></param>
    /// <returns></returns>
    private void PerformCleanUp(List<RemoteFile> remoteFiles)
    {
        // Encrypted files can be deleted as they have already been copied to the cloud and are no longer necessary.
        var di = new DirectoryInfo(DiskOtemporaryFolder);

        foreach (var file in di.EnumerateFiles())
        {
            file.Delete();
        }

        foreach (var dir in di.EnumerateDirectories())
        {
            dir.Delete(true);
        }
        
        if (DeleteOriginalFileCheckbox)
        {
            foreach (var file in remoteFiles) // Delete source files
            {
                File.Delete(file.OriginalFilePath);
            }
        }            
    }

    // TODO Extract single entry addition. Return type: list of remotefiles failed to be added.
    private static async Task<int> AddRemoteFilesToDatabase(List<RemoteFile> remoteFiles)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);
        var apiUrl = $"{BaseURL}Database";

        foreach (var remoteFile in remoteFiles)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(remoteFile);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            try
            {
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    // TODO: Log success.
                }
                else
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Не удалось добавить один или несколько файлов. \nДетали: \n{errorMessage}", "Критическая ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                    // TODO: Log fail.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось добавить один или несколько файлов. \nДетали: \n{ex.Message}", "Критическая ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }

        return 0;
    }

    private static async Task CopyFilesToCloud(List<RemoteFile> remoteFiles)
    {
        foreach (var remoteFile in remoteFiles)
        {
            await CopyFile(remoteFile.EncryptedFilePath, DiskOArchiveDestination);
        }
    }

    private static async Task EncryptFiles(List<RemoteFile> files)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);
        var apiUrl = $"{BaseURL}FileEncryptor";

        foreach (var file in files)
        {
            var request = new EncryptFileRequest()
            {
                EncryptionKey = file.EncryptionKey,
                IV = file.IV,
                InputFilePath = file.OriginalFilePath,
                EncryptedFilePath = file.EncryptedFilePath,
                EncryptedName = file.EncryptedFileName
            };

            try
            {
                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine("Status code is not successfull!");
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Error getting remote files: {ex.Message}");
            }
        }
    }

    private static async Task<List<RemoteFile>> GetRemoteFiles(string[] droppedFiles)
    {
        var remoteFiles = new List<RemoteFile>();

        foreach (var file in droppedFiles)
        {
            HttpResponseMessage response;

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);

            try
            {
                var apiUrl = $"{BaseURL}RemoteFilesProvider/GetRemoteFile";
                var json = JsonConvert.SerializeObject(file);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                response = await client.PostAsync(apiUrl, content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                var remoteFile = System.Text.Json.JsonSerializer.Deserialize<RemoteFile>(responseBody, _deserializationOptions);
                remoteFiles.Add(remoteFile);
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP request exceptions
                Console.WriteLine($"HTTP request failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // Log not processed files.
                throw;
            }
        }

        return remoteFiles;
    }

    private static async Task CopyFiles(IEnumerable<string> filePaths, string destinationFolder)
    {
        foreach (var filePath in filePaths)
        {
            await CopyFile(filePath, destinationFolder);
        }
    }

    private static async Task CopyFile(string filePath, string destinationFolder)
    {
        var request = new CopyFileRequest()
        {
            SourceFilePath = filePath,
            DestinationFolderPath = destinationFolder
        };

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);

        try
        {
            var apiUrl = $"{BaseURL}FileCopier";
            var json = System.Text.Json.JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error copying file: {ex.Message}");
        }
    }

    private void panel1_DragLeave(object sender, EventArgs e)
    {
        Activate();
        panel1.AllowDrop = true;
        panel1.BackColor = Color.MediumSlateBlue;
        panel1.Invalidate();
    }

    private void panel1_DragEnter(object sender, DragEventArgs e)
    {
        Activate();
        panel1.Invalidate();
        panel1.BackColor = Color.GreenYellow;
        e.Effect = DragDropEffects.Copy;
    }

    private async void panel1_DragDrop(object sender, DragEventArgs e)
    {
        var bad = await VerifyDatabase();

        Activate();
        //panel1.BackColor = Color.LightGreen;
        //panel1.Invalidate();
        //var collection = (string[])e.Data.GetData(DataFormats.FileDrop);
        //var x = 0;

        //var sessionFolder = await DecryptDroppedFiles(collection);
        //Process.Start("explorer.exe", $"/select,\"{sessionFolder}\"");

        //panel1.AllowDrop = true;
    }

    /// <summary>
    /// Retrieves a list of RemoteFiles from the database that do not have an existing file representation in the specified cloud folder.
    /// This method analyzes the specified cloud folder for file representations and compares them with entries in the database.
    /// Entries in the database that do not have a corresponding file in the cloud folder are considered "bad" entries and are returned in the list.
    /// </summary>
    /// <param name="analyseFolder">The folder to be analyzed for file representations. Default value is set to @"O:\Архив\2019".</param>
    /// <returns>A list of RemoteFile objects from the database that do not have a corresponding file in the cloud folder.</returns>
    private static async Task<List<RemoteFile>> GetBadEntries(string analyseFolder = @"O:\Архив\Мама")
    {
        analyseFolder = DiskOArchiveDestination;
        var entriesWithoutRepresentation = new List<RemoteFile>();

        // Get all entries from the database
        var allEntries = await GetAllFiles();

        foreach (var entry in allEntries)
        {
            var filePath = Path.Combine(analyseFolder, $"{entry.EncryptedFileName}.ppa");

            var partOfOriginalPath = @"D:\Архив\";
            // If file doesn't exist in the folder, add the entry to the list
            if (!File.Exists(filePath) && entry.OriginalFilePath.Contains($"{partOfOriginalPath}{DestinationFolder}"))
            {
                entriesWithoutRepresentation.Add(entry);
            }
        }

        return entriesWithoutRepresentation;
    }

    private static async Task<List<RemoteFile>> VerifyDatabase()
    {
        // Get all entries from the database
        var allEntries = await GetAllFiles();
        var invalidEntries = new List<RemoteFile>();

        foreach (var file in allEntries)
        {
            var partOfOriginalPath = @"D:\Архив\";
            var targetFolder = file.OriginalFilePath.Substring(partOfOriginalPath.Length, 4);
            var remoteFolderTargetFile = Path.Combine(@"O:\Архив\", targetFolder, $"{file.EncryptedFileName}.ppa");

            if (!(File.Exists(file.OriginalFilePath) && File.Exists(remoteFolderTargetFile)))
            {
                
                invalidEntries.Add(file);
            }
        }

        return invalidEntries;
    }

    private static async Task<string> DecryptDroppedFiles(string[] files)
    {
        var decryptedFilesPath = Path.Combine(TemporaryFolder, "Decrypted");

        var sessionFolder = Path.Combine(decryptedFilesPath, Path.GetRandomFileName());
        Directory.CreateDirectory(sessionFolder);

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);

        foreach (var file in files)
        {
            var remoteFile = await GetEntryByEncryptedFileName(file);
            var originalFileName = remoteFile.OriginalFileName;
            var decryptedFilePath = Path.Combine(sessionFolder, originalFileName);
            var request = new DecryptFileRequest
            {
                EncryptedFilePath = file,
                DecryptedFilePath = decryptedFilePath,
                EncryptionKey = remoteFile.EncryptionKey,
                IV = remoteFile.IV
            };

            var apiUrl = $"{BaseURL}FileEncryptor/decrypt";
            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync(apiUrl, content);
            response.EnsureSuccessStatusCode();
        }

        return sessionFolder;
    }

    private static async Task<RemoteFile> GetEntryByEncryptedFileName(string file)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);
        try
        {
            var encryptedFileName = Path.GetFileNameWithoutExtension(file);
            var apiUrl = $"{BaseURL}Database/GetByEncryptedName" +
                     $"?EncryptedFileName={Uri.EscapeDataString(encryptedFileName)}";

            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<RemoteFile>(responseBody);

                return result;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Handle response here
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Error retreiving RemoteFile: {ex.Message}");
            
        }

        return null;
    }

    public static async Task<List<RemoteFile>> GetAllFiles()
    {
        var allFiles = new List<RemoteFile>();
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(_httpClientTimemout);

        try
        {
            var apiUrl = $"{BaseURL}Database/GetAll";
            var response = await client.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<List<RemoteFile>>(responseBody);

                return result;
            }
            else
            {
                // Handle response here
            }
        }
        catch (Exception ex)
        {
            // Handle exception
            Console.WriteLine($"Error: {ex.Message}");
        }

        return allFiles;
    }

    private void InitializeTargetFolderCombobox()
    {
        targetFolderComboBox.DataSource = GetFoldersForComboBox();

        List<string> GetFoldersForComboBox()
        {
            var folderNames = new List<string>();

            try
            {
                if (Directory.Exists(LocalArchiveRoot))
                {
                    // Get a list of all subdirectories (folders) within the specified directory
                    var subDirectories = Directory.GetDirectories(LocalArchiveRoot);

                    // Extract just the folder names from the full paths
                    foreach (var subDirectory in subDirectories)
                    {
                        folderNames.Add(Path.GetFileName(subDirectory));
                    }
                }
                else
                {
                    MessageBox.Show("Указанная папка не существует.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }

            return folderNames;
        }
    }

    private void targetFolderComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        DestinationFolder = targetFolderComboBox.Text;
    }

    private void DisplayCurrentYearInCombobox()
    {
        try
        {
            var currentYear = DateTime.Now.Year;

            // Check if there is a folder with the current year as its name
            if (targetFolderComboBox.Items.Contains(currentYear.ToString()))
            {
                // Set the current year as the selected item in the combobox
                targetFolderComboBox.SelectedItem = currentYear.ToString();
                DestinationFolder = currentYear.ToString();
            }
            else
            {
                // If there is no folder for the current year, set the first folder in the combobox as the selected item
                if (targetFolderComboBox.Items.Count > 0)
                {
                    targetFolderComboBox.SelectedIndex = 0;
                    DestinationFolder = targetFolderComboBox.SelectedItem.ToString();
                }
                else
                {
                    // If there are no folders in the combobox, set the destination folder to a default value
                    _destinationFolder = "2019";
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("An error occurred while setting up the destination folder: " + ex.Message);
        }
    }
    }
