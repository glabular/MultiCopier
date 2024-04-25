using API.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;
[Route("api/[controller]")]
[ApiController]
public partial class FileCopierController : ControllerBase
{
    [HttpPost]
    public IActionResult CopyFile([FromBody] CopyFileRequest request)
    {
        try
        {
            var destinationFilePath = Path.Combine(request.DestinationFolderPath, Path.GetFileName(request.SourceFilePath));
            System.IO.File.Copy(request.SourceFilePath, destinationFilePath);

            return Ok("File copied successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }
}