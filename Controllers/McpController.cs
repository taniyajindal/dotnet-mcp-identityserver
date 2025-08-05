using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using McpAuthServer.Services;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class McpController : ControllerBase
{
    private readonly IWeatherService _weatherService;
    private readonly IClaudeService _claudeService;
    private readonly ILogger<McpController> _logger;

    public McpController(IWeatherService weatherService, IClaudeService claudeService, ILogger<McpController> logger)
    {
        _weatherService = weatherService;
        _claudeService = claudeService;
        _logger = logger;
    }
    [HttpPost("initialize")]
    public IActionResult Initialize([FromBody] InitializeRequest request)
    {
        var response = new
        {
            protocolVersion = "2024-11-05",
            capabilities = new
            {
                tools = new { },
                resources = new { }
            },
            serverInfo = new
            {
                name = "MCP Auth Server",
                version = "1.0.0"
            }
        };

        return Ok(response);
    }

    [HttpGet("tools")]
    public IActionResult GetTools()
    {
        var tools = new
        {
            tools = new object[]
            {
                new
                {
                    name = "get_user_info",
                    description = "Get current user information",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = new string[0]
                    }
                },
                new
                {
                    name = "get_user_claims",
                    description = "Get current user claims",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new { },
                        required = new string[0]
                    }
                },
                new
                {
                    name = "get_weather",
                    description = "Get current weather for a city",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            city = new
                            {
                                type = "string",
                                description = "Name of the city to get weather for"
                            }
                        },
                        required = new[] { "city" }
                    }
                },
                new
                {
                    name = "chat_with_claude",
                    description = "Chat with Claude AI assistant",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            message = new
                            {
                                type = "string",
                                description = "Message to send to Claude"
                            },
                            system_prompt = new
                            {
                                type = "string",
                                description = "Optional system prompt"
                            }
                        },
                        required = new[] { "message" }
                    }
                },
                new
                {
                    name = "claude_with_tools",
                    description = "Chat with Claude AI that can use tools like weather",
                    inputSchema = new
                    {
                        type = "object",
                        properties = new
                        {
                            message = new
                            {
                                type = "string",
                                description = "Message to send to Claude"
                            }
                        },
                        required = new[] { "message" }
                    }
                }
            }
        };

        return Ok(tools);
    }

    [HttpPost("tools/call")]
    public async Task<IActionResult> CallTool([FromBody] ToolCallRequest request)
    {
        var user = HttpContext.User;

        return request.Name switch
        {
            "get_user_info" => Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"User: {user.Identity?.Name ?? "Unknown"}\nUser ID: {user.FindFirst("sub")?.Value ?? "N/A"}\nEmail: {user.FindFirst("email")?.Value ?? "N/A"}"
                    }
                }
            }),
            "get_user_claims" => Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = string.Join("\n", user.Claims.Select(c => $"{c.Type}: {c.Value}"))
                    }
                }
            }),
            "get_weather" => await HandleWeatherTool(request.Arguments),
            "chat_with_claude" => await HandleClaudeChatTool(request.Arguments),
            "claude_with_tools" => await HandleClaudeWithToolsTool(request.Arguments, user),
            _ => BadRequest(new { error = $"Unknown tool: {request.Name}" })
        };
    }

    private async Task<IActionResult> HandleWeatherTool(object? arguments)
    {
        try
        {
            var city = ExtractCityFromArguments(arguments);
            if (string.IsNullOrEmpty(city))
            {
                return BadRequest(new { error = "City parameter is required" });
            }

            var userId = HttpContext.User.FindFirst("sub")?.Value ?? HttpContext.User.Identity?.Name;
            var weather = await _weatherService.GetWeatherAsync(city, userId);
            if (weather == null)
            {
                return Ok(new
                {
                    content = new[]
                    {
                        new
                        {
                            type = "text",
                            text = $"Unable to get weather data for {city}. Please check the city name and try again."
                        }
                    }
                });
            }

            var userName = HttpContext.User.Identity?.Name ?? HttpContext.User.FindFirst("firstName")?.Value ?? "there";
            var weatherText = $"Hi {userName}! 🌤️ Here's the weather in {weather.City}, {weather.Country}\n" +
                             $"Temperature: {weather.Temperature}{weather.TemperatureUnit}\n" +
                             $"Condition: {weather.Description}\n" +
                             $"Humidity: {weather.Humidity}%\n" +
                             $"Pressure: {weather.Pressure} hPa\n" +
                             $"Wind Speed: {weather.WindSpeed} m/s\n" +
                             $"Cloud Cover: {weather.CloudCover}%";

            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = weatherText
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Error getting weather data: {ex.Message}"
                    }
                }
            });
        }
    }

    private async Task<IActionResult> HandleClaudeChatTool(object? arguments)
    {
        try
        {
            var message = ExtractStringFromArguments(arguments, "message");
            var systemPrompt = ExtractStringFromArguments(arguments, "system_prompt");
            
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest(new { error = "Message parameter is required" });
            }

            var response = await _claudeService.ChatAsync(message, systemPrompt);
            
            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"🤖 Claude says:\n{response}"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Error chatting with Claude: {ex.Message}"
                    }
                }
            });
        }
    }

    private async Task<IActionResult> HandleClaudeWithToolsTool(object? arguments, System.Security.Claims.ClaimsPrincipal user)
    {
        try
        {
            var message = ExtractStringFromArguments(arguments, "message");
            
            if (string.IsNullOrEmpty(message))
            {
                return BadRequest(new { error = "Message parameter is required" });
            }

            var userId = user.FindFirst("sub")?.Value ?? user.Identity?.Name ?? "unknown";
            
            _logger.LogInformation("Claude with tools: userId={UserId}", userId);
            
            var response = await _claudeService.ChatWithToolsAsync(message, userId);
            
            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"🧠 Claude with tools:\n{response}"
                    }
                }
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                content = new[]
                {
                    new
                    {
                        type = "text",
                        text = $"Error using Claude with tools: {ex.Message}"
                    }
                }
            });
        }
    }

    private string ExtractCityFromArguments(object? arguments)
    {
        return ExtractStringFromArguments(arguments, "city");
    }


    private string ExtractStringFromArguments(object? arguments, string propertyName)
    {
        if (arguments is System.Text.Json.JsonElement element && 
            element.TryGetProperty(propertyName, out var propertyElement))
        {
            return propertyElement.GetString() ?? "";
        }
        return "";
    }

    [HttpGet("resources")]
    public IActionResult GetResources()
    {
        var resources = new
        {
            resources = new[]
            {
                new
                {
                    uri = "user://profile",
                    name = "User Profile",
                    description = "Current user profile information"
                },
                new
                {
                    uri = "user://permissions",
                    name = "User Permissions",
                    description = "Current user permissions and roles"
                }
            }
        };

        return Ok(resources);
    }

    [HttpPost("resources/read")]
    public IActionResult ReadResource([FromBody] ResourceReadRequest request)
    {
        var user = HttpContext.User;

        return request.Uri switch
        {
            "user://profile" => Ok(new
            {
                contents = new[]
                {
                    new
                    {
                        uri = "user://profile",
                        mimeType = "application/json",
                        text = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            userId = user.FindFirst("sub")?.Value,
                            name = user.Identity?.Name,
                            email = user.FindFirst("email")?.Value,
                            roles = user.FindAll("role").Select(c => c.Value).ToArray()
                        })
                    }
                }
            }),
            "user://permissions" => Ok(new
            {
                contents = new[]
                {
                    new
                    {
                        uri = "user://permissions",
                        mimeType = "application/json",
                        text = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            roles = user.FindAll("role").Select(c => c.Value).ToArray(),
                            scopes = user.FindAll("scope").Select(c => c.Value).ToArray(),
                            permissions = user.FindAll("permission").Select(c => c.Value).ToArray()
                        })
                    }
                }
            }),
            _ => NotFound(new { error = $"Resource not found: {request.Uri}" })
        };
    }
}

public record InitializeRequest(string ProtocolVersion, object? ClientInfo);
public record ToolCallRequest(string Name, object? Arguments);
public record ResourceReadRequest(string Uri);