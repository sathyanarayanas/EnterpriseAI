using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseAI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _chatService.SendMessageAsync(
                request,
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid chat request.");

            return BadRequest(new
            {
                error = ex.Message
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Chat request was cancelled.");

            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while processing chat request.");

            return StatusCode(500, new
            {
                error = "An unexpected error occurred."
            });
        }
    }
}