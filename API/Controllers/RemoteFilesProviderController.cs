using API.Interfaces;
using API.Models;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class RemoteFilesProviderController : ControllerBase
{
    private readonly IHashCalculator _hashCalculator;
    private readonly IFileEncryptor _fileEncryptor;
    private readonly ILogger<RemoteFilesProviderController> _logger;

    public RemoteFilesProviderController(IHashCalculator hashCalculator, IFileEncryptor fileEncryptor, ILogger<RemoteFilesProviderController> logger)
    {
        _hashCalculator = hashCalculator;
        _fileEncryptor = fileEncryptor;
        _logger = logger;
    }

    [HttpPost("GetRemoteFile")]
    public IActionResult GetRemoteFile([FromBody] string file)
    {
        var processingFile = Path.GetFileName(file);
        _logger.LogInformation($"Processing file: {processingFile}");

        try
        {
            var encryptedFileName = GenerateId();
            _logger.LogInformation($"Generated EncryptedFileName for {processingFile}: {encryptedFileName}");

            var sha512 = _hashCalculator.GetSHA512(file);
            _logger.LogInformation($"Generated SHA512 for {processingFile}: {sha512}");

            var encryptionKey = _fileEncryptor.GenerateEncryptionKey(32);
            _logger.LogInformation($"Generated EncryptionKey for {processingFile}: {Convert.ToBase64String(encryptionKey)}");

            var iv = _fileEncryptor.GenerateEncryptionKey(16);
            _logger.LogInformation($"Generated IV for {processingFile}: {Convert.ToBase64String(iv)}");

            var randomTempFolder = GetRandomTempFolder();
            _logger.LogInformation($"Temporary folder created: {randomTempFolder}");
            var encryptedFilePath = Path.Combine(randomTempFolder, $"{encryptedFileName}.ppa");

            var remoteFile = new RemoteFile
            {
                OriginalFileName = processingFile,
                SHA512 = sha512,
                EncryptionKey = encryptionKey,
                IV = iv,
                EncryptedFileName = encryptedFileName,
                EncryptedFilePath = encryptedFilePath,
                OriginalFilePath = file,
                UploadTimestamp = DateTime.Now
            };

            _logger.LogInformation($"Metadata for file {processingFile} created.");

            return Ok(remoteFile);
        }
        catch (Exception ex)
        {
            // LOG
            _logger.LogError(ex, "An error occurred while processing the file.");
            return BadRequest("An error occurred while processing the file.");
        }
    }

    /// <summary>
    /// Generates a unique identifier by concatenating two GUIDs together.
    /// </summary>
    /// <remarks>
    /// This method ensures a high level of uniqueness by combining two globally unique identifiers (GUIDs).
    /// </remarks>
    /// <returns>A string representing the unique identifier.</returns>
    private static string GenerateId()
    {
        var firstGuid = Guid.NewGuid();
        var secondGuid = Guid.NewGuid();

        return firstGuid.ToString("N") + secondGuid.ToString("N");
    }

    private string GetRandomTempFolder()
    {
        // TODO remove hardcoded value
        var baseFolder = @"D:\temp\MultiCopierTMP\Disk-O";

        string randomFolderName;
        string tempFolder;

        // Loop until a unique folder name is generated or maximum attempts reached
        for (var attempts = 0; attempts < 100; attempts++)
        {
            randomFolderName = Guid.NewGuid().ToString();
            tempFolder = Path.Combine(baseFolder, randomFolderName);

            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);

                // Remove the read-only attribute from the directory if it exists
                var di = new DirectoryInfo(tempFolder);
                di.Attributes &= ~FileAttributes.ReadOnly;

                return tempFolder;
            }
        }

        // If maximum attempts reached without creating a unique folder name, throw an exception
        throw new InvalidOperationException("Exceeded attempts to create a unique folder name.");
    }
}
