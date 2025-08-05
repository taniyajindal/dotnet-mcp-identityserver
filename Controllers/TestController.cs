using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [HttpPost("decode-jwt")]
    [AllowAnonymous]
    public IActionResult DecodeJwt([FromBody] TokenRequest request)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(request.Token);
            
            return Ok(new
            {
                header = token.Header,
                payload = token.Claims.Select(c => new { type = c.Type, value = c.Value }),
                issuer = token.Issuer,
                audiences = token.Audiences,
                validFrom = token.ValidFrom,
                validTo = token.ValidTo
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("auth-config")]
    [AllowAnonymous]
    public IActionResult GetAuthConfig()
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var identitySection = config.GetSection("IdentityServer");
        
        return Ok(new
        {
            authority = identitySection["Authority"],
            audience = identitySection["Audience"],
            requireHttpsMetadata = identitySection.GetValue<bool>("RequireHttpsMetadata")
        });
    }

    [HttpGet("well-known-test")]
    [AllowAnonymous]
    public async Task<IActionResult> TestWellKnown()
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var authority = config["IdentityServer:Authority"];
        
        try
        {
            var httpClient = HttpContext.RequestServices.GetRequiredService<HttpClient>();
            var discoveryUrl = $"{authority}/.well-known/openid_configuration";
            
            var response = await httpClient.GetStringAsync(discoveryUrl);
            return Ok(new { discoveryUrl, response });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, authority });
        }
    }
}

public record TokenRequest(string Token);