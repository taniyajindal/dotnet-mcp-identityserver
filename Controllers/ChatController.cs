using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using McpAuthServer.Services;
using System.Security.Claims;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IClaudeService claudeService, ILogger<ChatController> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    [HttpPost("completions")]
    public async Task<IActionResult> ChatCompletions([FromBody] ChatRequest request)
    {
        try
        {
            var user = HttpContext.User;
            var userId = user.FindFirst("sub")?.Value ?? user.Identity?.Name ?? "unknown";
            var userName = user.Identity?.Name ?? user.FindFirst("firstName")?.Value ?? user.FindFirst("name")?.Value ?? "User";

            _logger.LogInformation("ChatCompletions: userId={UserId}", userId);

            string response;
            
            if (request.UseTools)
            {
                // Pass user context for tools
                var userContext = $"{userName} (ID: {userId})";
                response = await _claudeService.ChatWithToolsAsync(request.Message, userContext);
            }
            else
            {
                var systemPrompt = string.IsNullOrEmpty(request.SystemPrompt) 
                    ? $"You are a helpful assistant talking to {userName}. Always address them by name when appropriate and be conversational and friendly."
                    : request.SystemPrompt;
                    
                response = await _claudeService.ChatAsync(request.Message, systemPrompt);
            }

            return Ok(new ChatResponse
            {
                Message = response,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow,
                Model = request.UseTools ? "claude-with-tools" : "claude-chat"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("stream")]
    public async Task<IActionResult> StreamChat([FromBody] ChatRequest request)
    {
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        try
        {
            var user = HttpContext.User;
            var userId = user.FindFirst("sub")?.Value ?? user.Identity?.Name ?? "unknown";
            
            _logger.LogInformation("ChatController: userId={UserId}", userId);

            // Simulate streaming by chunking the response
            string response;
            if (request.UseTools)
            {
                response = await _claudeService.ChatWithToolsAsync(request.Message, userId);
            }
            else
            {
                response = await _claudeService.ChatAsync(request.Message, request.SystemPrompt);
            }

            // Split response into chunks for streaming effect
            var words = response.Split(' ');
            foreach (var word in words)
            {
                await Response.WriteAsync($"data: {word} \n\n");
                await Response.Body.FlushAsync();
                await Task.Delay(50); // Simulate streaming delay
            }

            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            await Response.WriteAsync($"data: Error: {ex.Message}\n\n");
            return new EmptyResult();
        }
    }

    [HttpGet("models")]
    public IActionResult GetModels()
    {
        return Ok(new
        {
            models = new[]
            {
                new { id = "claude-3-sonnet-20240229", name = "Claude 3 Sonnet" },
                new { id = "claude-3-haiku-20240307", name = "Claude 3 Haiku" },
                new { id = "claude-with-tools", name = "Claude with MCP Tools" }
            }
        });
    }

    [HttpGet("conversation/history")]
    public IActionResult GetConversationHistory()
    {
        var user = HttpContext.User;
        var userId = user.FindFirst("sub")?.Value ?? "unknown";

        // In a real app, you'd fetch from database
        return Ok(new
        {
            userId,
            conversations = new[]
            {
                new
                {
                    id = "demo-1",
                    title = "Demo Conversation",
                    lastMessage = "This is a demo conversation history",
                    timestamp = DateTime.UtcNow.AddHours(-1)
                }
            },
            message = "Demo mode - connect real storage for conversation history"
        });
    }
}

public record ChatRequest(string Message, string? SystemPrompt = null, bool UseTools = false);

public record ChatResponse
{
    public string Message { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public string Model { get; init; } = string.Empty;
}