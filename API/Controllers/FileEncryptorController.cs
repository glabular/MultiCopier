using API.Interfaces;
using API.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class FileEncryptorController : ControllerBase
{
    private readonly IFileEncryptor _fileEncryptor;
    private readonly ILogger<FileEncryptorController> _logger;

    public FileEncryptorController(IFileEncryptor fileEncryptor, ILogger<FileEncryptorController> logger)
    {
        _fileEncryptor = fileEncryptor;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> EncryptFile([FromBody] EncryptFileRequest request)
    {
        var validationResult = ValidateFileEncryptionRequest(request);

        if (validationResult != null)
        {
            return validationResult;
        }

        try
        {
            var processingFile = Path.GetFileName(request.InputFilePath);

            _logger.LogInformation($"Started encrypting the file: {processingFile}");

            await _fileEncryptor.EncryptFileAsync(request.InputFilePath,
                                                request.EncryptionKey,
                                                request.IV,
                                                request.EncryptedName,
            request.EncryptedFilePath);

            _logger.LogInformation($"Encryption of the file '{processingFile}' completed successfully.");
            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpPost("decrypt")]
    public async Task<IActionResult> DecryptFile([FromBody] DecryptFileRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.EncryptedFilePath) ||
            string.IsNullOrWhiteSpace(request.DecryptedFilePath) ||
            request.EncryptionKey == null || request.IV == null)
        {
            return BadRequest("Invalid request. Please provide all required parameters.");
        }

        try
        {
            await _fileEncryptor.DecryptFileAsync(request.EncryptedFilePath,
                                                  request.EncryptionKey,
                                                  request.IV,
                                                  request.DecryptedFilePath);

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    private BadRequestObjectResult ValidateFileEncryptionRequest(EncryptFileRequest request)
    {
        if (request == null)
        {
            _logger.LogError("Invalid request. Request object is null.");
            return BadRequest("Invalid request. Please provide all required parameters.");
        }

        if (string.IsNullOrWhiteSpace(request.InputFilePath))
        {
            _logger.LogError("Invalid request. InputFilePath is null or empty.");
            return BadRequest("Invalid request. InputFilePath is null or empty.");
        }

        if (string.IsNullOrWhiteSpace(request.EncryptedFilePath))
        {
            _logger.LogError("Invalid request. EncryptedFilePath is null or empty.");
            return BadRequest("Invalid request. EncryptedFilePath is null or empty.");
        }

        if (string.IsNullOrWhiteSpace(request.EncryptedName))
        {
            _logger.LogError("Invalid request. EncryptedName is null or empty.");
            return BadRequest("Invalid request. EncryptedName is null or empty.");
        }

        if (request.EncryptionKey == null)
        {
            _logger.LogError("Invalid request. EncryptionKey is null.");
            return BadRequest("Invalid request. EncryptionKey is null.");
        }

        if (request.IV == null)
        {
            _logger.LogError("Invalid request. IV is null.");
            return BadRequest("Invalid request. IV is null.");
        }

        return null;
    }
}
