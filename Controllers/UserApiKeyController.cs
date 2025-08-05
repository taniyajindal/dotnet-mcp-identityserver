using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using McpAuthServer.Services;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserApiKeyController : ControllerBase
{
    private readonly IUserApiKeyService _userApiKeyService;

    public UserApiKeyController(IUserApiKeyService userApiKeyService)
    {
        _userApiKeyService = userApiKeyService;
    }

    [HttpGet("weather")]
    public async Task<IActionResult> GetWeatherApiKey()
    {
        var userId = HttpContext.User.FindFirst("sub")?.Value ?? HttpContext.User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User ID not found" });
        }

        var apiKey = await _userApiKeyService.GetWeatherApiKeyAsync(userId);
        var isPremium = await _userApiKeyService.IsUserPremiumAsync(userId);
        var role = await _userApiKeyService.GetUserRoleAsync(userId);

        return Ok(new
        {
            userId,
            hasCustomApiKey = !string.IsNullOrEmpty(apiKey),
            isPremium,
            role,
            // Don't return the actual API key for security
            apiKeyType = apiKey switch
            {
                null => "default",
                "open-meteo" => "open-meteo",
                var key when key.StartsWith("premium") => "premium",
                var key when key.StartsWith("admin") => "admin",
                _ => "custom"
            }
        });
    }

    [HttpPost("weather")]
    public async Task<IActionResult> SetWeatherApiKey([FromBody] SetApiKeyRequest request)
    {
        var userId = HttpContext.User.FindFirst("sub")?.Value ?? HttpContext.User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User ID not found" });
        }

        if (string.IsNullOrEmpty(request.ApiKey))
        {
            return BadRequest(new { error = "API key is required" });
        }

        await _userApiKeyService.SetWeatherApiKeyAsync(userId, request.ApiKey);

        return Ok(new
        {
            message = "Weather API key updated successfully",
            userId
        });
    }

    [HttpDelete("weather")]
    public async Task<IActionResult> RemoveWeatherApiKey()
    {
        var userId = HttpContext.User.FindFirst("sub")?.Value ?? HttpContext.User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User ID not found" });
        }

        // Set to empty string to remove custom key
        await _userApiKeyService.SetWeatherApiKeyAsync(userId, "");

        return Ok(new
        {
            message = "Weather API key removed, will use default",
            userId
        });
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetApiKeyStatus()
    {
        var userId = HttpContext.User.FindFirst("sub")?.Value ?? HttpContext.User.Identity?.Name;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(new { error = "User ID not found" });
        }

        var weatherApiKey = await _userApiKeyService.GetWeatherApiKeyAsync(userId);
        var isPremium = await _userApiKeyService.IsUserPremiumAsync(userId);
        var role = await _userApiKeyService.GetUserRoleAsync(userId);

        return Ok(new
        {
            userId,
            userName = HttpContext.User.Identity?.Name,
            weather = new
            {
                hasCustomKey = !string.IsNullOrEmpty(weatherApiKey),
                keyType = weatherApiKey switch
                {
                    null => "default (open-meteo)",
                    "open-meteo" => "open-meteo (free)",
                    var key when key.StartsWith("premium") => "premium tier",
                    var key when key.StartsWith("admin") => "admin tier",
                    _ => "custom key"
                }
            },
            account = new
            {
                isPremium,
                role = role ?? "user"
            }
        });
    }
}

public record SetApiKeyRequest(string ApiKey);