using API.Interfaces;
using API.Models;
using API.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class DatabaseController : ControllerBase
{
    private readonly IDBContext _dbContext;

    public DatabaseController(IDBContext dBContext)
    {
        _dbContext = dBContext;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var allFiles = await _dbContext.GetAll();

            if (allFiles.Count > 0)
            {
                return Ok(allFiles);
            }
            else
            {
                return NotFound("No files found in the database.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
        }
    }

    [HttpGet("GetByEncryptedName")]
    public async Task<IActionResult> GetByEncryptedName([FromQuery] string encryptedFileName)
    {
        ArgumentNullException.ThrowIfNull(encryptedFileName);

        RemoteFile endpointResponse;

        try
        {
            endpointResponse = await _dbContext.GetByEncryptedFileName(encryptedFileName);
        }
        catch (Exception ex)
        {

            throw;
        }

        if (endpointResponse == null)
        {
            return NotFound("RemoteFile not found.");
        }
        else
        {
            return Ok(endpointResponse);
        }
    }

    [HttpGet("GetById/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (id <= 0)
        {
            return BadRequest("Invalid ID.");
        }

        try
        {
            var remoteFile = await _dbContext.GetById(id);

            if (remoteFile == null)
            {
                return NotFound("RemoteFile not found.");
            }
            else
            {
                return Ok(remoteFile);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddEntry([FromBody] RemoteFile remoteFile)
    {
        if (remoteFile == null)
        {
            return BadRequest("Invalid request.");
        }

        // A single entry addition.
        try
        {
            await _dbContext.InsertRemoteFile(remoteFile);

            return Ok("Entries added successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpDelete("DeleteByEncryptedName/{encryptedFileName}")]
    public async Task<IActionResult> DeleteEntryByEncryptedName(string encryptedFileName)
    {
        try
        {
            // Check if the entry exists
            var existingEntry = await _dbContext.GetByEncryptedFileName(encryptedFileName);
            if (existingEntry == null)
            {
                return NotFound("Entry not found.");
            }

            await _dbContext.DeleteRemoteFile(encryptedFileName);

            return Ok("Entry deleted successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error: {ex.Message}");
        }
    }
}
