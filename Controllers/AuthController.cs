using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            service = "MCP Auth Server",
            version = "1.0.0",
            timestamp = DateTime.UtcNow,
            authenticated = User.Identity?.IsAuthenticated == true
        });
    }

    [HttpGet("userinfo")]
    [Authorize]
    public IActionResult GetUserInfo()
    {
        var user = HttpContext.User;

        return Ok(new
        {
            isAuthenticated = user.Identity?.IsAuthenticated == true,
            name = user.Identity?.Name,
            userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value,
            roles = user.FindAll("role").Select(c => c.Value).ToArray(),
            claims = user.Claims.Select(c => new { type = c.Type, value = c.Value }).ToArray()
        });
    }

    [HttpGet("test")]
    [Authorize]
    public IActionResult TestAuth()
    {
        return Ok(new
        {
            message = "Authentication successful!",
            user = User.Identity?.Name,
            timestamp = DateTime.UtcNow
        });
    }
}