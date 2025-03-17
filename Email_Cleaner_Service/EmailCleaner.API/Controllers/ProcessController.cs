namespace EmailCleaner.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using EmailCleaner.Infrastructure.Services;
using EmailCleaner.Infrastructure.Messaging;

[ApiController]
[Route("api/process")]
public class ProcessController : ControllerBase
{
    private readonly EmailProcessor _emailProcessor;

    public ProcessController(EmailProcessor emailProcessor)
    {
        _emailProcessor = emailProcessor;
    }

    [HttpPost]
    public IActionResult ProcessEmails([FromQuery] string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return BadRequest("Invalid or missing folder path.");
        }

        try
        {
            _emailProcessor.ProcessEmails(folderPath);
            return Ok("Emails processed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing emails: {ex.Message}");
            return StatusCode(500, "An error occurred while processing emails.");
        }
    }
}