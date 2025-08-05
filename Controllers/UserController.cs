using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    [HttpGet("details")]
    public IActionResult GetUserDetails()
    {
        var user = HttpContext.User;
        
        var userDetails = new
        {
            // Basic Identity Information
            identity = new
            {
                isAuthenticated = user.Identity?.IsAuthenticated ?? false,
                name = user.Identity?.Name,
                authenticationType = user.Identity?.AuthenticationType
            },
            
            // All Claims
            claims = user.Claims.Select(c => new
            {
                type = c.Type,
                value = c.Value,
                issuer = c.Issuer,
                originalIssuer = c.OriginalIssuer,
                valueType = c.ValueType
            }).ToArray(),
            
            // Common Claims (easy access)
            commonClaims = new
            {
                userId = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                userName = user.Identity?.Name,
                email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value,
                firstName = user.FindFirst("firstName")?.Value ?? user.FindFirst(ClaimTypes.GivenName)?.Value,
                lastName = user.FindFirst("lastName")?.Value ?? user.FindFirst(ClaimTypes.Surname)?.Value,
                role = user.FindFirst("role")?.Value ?? user.FindFirst(ClaimTypes.Role)?.Value,
                roles = user.FindAll("role").Select(c => c.Value).ToArray(),
                scopes = user.FindAll("scope").Select(c => c.Value).ToArray()
            },
            
            // Custom Claims from your Identity Server
            customClaims = new
            {
                companyId = user.FindFirst("companyId")?.Value,
                domainId = user.FindFirst("domainId")?.Value,
                coreSuperUser = user.FindFirst("coreSuperUser")?.Value,
                authTime = user.FindFirst("auth_time")?.Value,
                idp = user.FindFirst("idp")?.Value,
                sessionId = user.FindFirst("sid")?.Value,
                jwtId = user.FindFirst("jti")?.Value
            },
            
            // HTTP Context Information
            context = new
            {
                requestId = HttpContext.TraceIdentifier,
                remoteIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault(),
                authorizationHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Substring(0, Math.Min(20, HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Length ?? 0)) + "...",
                requestPath = HttpContext.Request.Path,
                requestMethod = HttpContext.Request.Method
            },
            
            // Token Information
            tokenInfo = new
            {
                hasAuthorizationHeader = HttpContext.Request.Headers.ContainsKey("Authorization"),
                tokenType = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').FirstOrDefault(),
                tokenLength = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(' ').LastOrDefault()?.Length ?? 0
            },
            
            // Timestamp
            timestamp = DateTime.UtcNow,
            serverInfo = new
            {
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                dotnetVersion = Environment.Version.ToString()
            }
        };

        return Ok(userDetails);
    }

    [HttpGet("claims")]
    public IActionResult GetUserClaims()
    {
        var user = HttpContext.User;
        
        var claims = user.Claims.Select(c => new
        {
            type = c.Type,
            value = c.Value,
            issuer = c.Issuer,
            // Format claim type for readability
            friendlyType = c.Type switch
            {
                "sub" => "Subject (User ID)",
                "email" => "Email Address",
                "name" => "Display Name",
                "role" => "User Role",
                "scope" => "Permission Scope",
                "aud" => "Audience",
                "iss" => "Issuer",
                "exp" => "Expires At",
                "iat" => "Issued At",
                "auth_time" => "Authentication Time",
                "jti" => "JWT ID",
                "sid" => "Session ID",
                "idp" => "Identity Provider",
                var type when type.Contains("nameidentifier") => "Name Identifier",
                var type when type.Contains("givenname") => "First Name",
                var type when type.Contains("surname") => "Last Name",
                _ => c.Type
            }
        }).OrderBy(c => c.friendlyType).ToArray();

        return Ok(new
        {
            userId = user.FindFirst("sub")?.Value,
            userName = user.Identity?.Name,
            totalClaims = claims.Length,
            claims = claims,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("summary")]
    public IActionResult GetUserSummary()
    {
        var user = HttpContext.User;
        
        return Ok(new
        {
            user = new
            {
                id = user.FindFirst("sub")?.Value,
                name = user.Identity?.Name,
                email = user.FindFirst("email")?.Value,
                firstName = user.FindFirst("firstName")?.Value,
                lastName = user.FindFirst("lastName")?.Value,
                fullName = $"{user.FindFirst("firstName")?.Value} {user.FindFirst("lastName")?.Value}".Trim()
            },
            company = new
            {
                id = user.FindFirst("companyId")?.Value,
                domain = user.FindFirst("domainId")?.Value
            },
            permissions = new
            {
                role = user.FindFirst("role")?.Value,
                isSuperUser = user.FindFirst("coreSuperUser")?.Value == "True",
                scopes = user.FindAll("scope").Select(c => c.Value).ToArray()
            },
            session = new
            {
                authTime = user.FindFirst("auth_time")?.Value,
                sessionId = user.FindFirst("sid")?.Value,
                idp = user.FindFirst("idp")?.Value
            },
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("test-echo")]
    public IActionResult TestEcho([FromBody] object data)
    {
        var user = HttpContext.User;
        
        return Ok(new
        {
            message = $"Hello {user.Identity?.Name ?? "Unknown User"}!",
            userId = user.FindFirst("sub")?.Value,
            receivedData = data,
            timestamp = DateTime.UtcNow
        });
    }
}