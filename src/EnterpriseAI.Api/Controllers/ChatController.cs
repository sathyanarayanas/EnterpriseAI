
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.Models;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

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
    [RequestTimeout(milliseconds: 90000)]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId =
            Activity.Current?.TraceId.ToString()
            ?? HttpContext.TraceIdentifier;

        try
        {
            _logger.LogInformation(
                "Chat API request received. CorrelationId: {CorrelationId}",
                correlationId);

            var response = await _chatService.SendMessageAsync(
                request,
                cancellationToken);

            _logger.LogInformation(
                "Chat API request completed. CorrelationId: {CorrelationId}",
                correlationId);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid chat request. CorrelationId: {CorrelationId}",
                correlationId);

            return BadRequest(new
            {
                error = ex.Message
            });
        }
        catch (OperationCanceledException)
        {
            var timeoutFeature =
                HttpContext.Features.Get<IHttpRequestTimeoutFeature>();

            if (timeoutFeature?.RequestTimeoutToken.IsCancellationRequested == true)
            {
                _logger.LogWarning(
                    "Chat request timed out. CorrelationId: {CorrelationId}",
                    correlationId);

                return StatusCode(504, new
                {
                    error = "The chat request timed out. Please try again."
                });
            }

            _logger.LogInformation(
                "Chat request was cancelled. CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(499);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while processing chat request. CorrelationId: {CorrelationId}",
                correlationId);

            return StatusCode(500, new
            {
                error = "An unexpected error occurred."
            });
        }
    }
}
