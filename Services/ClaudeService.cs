using System.Text.Json;

namespace McpAuthServer.Services;

public interface IClaudeService
{
    Task<string> ChatAsync(string message, string? systemPrompt = null);
    Task<string> ChatWithToolsAsync(string message, string userId);
}

public class ClaudeService : IClaudeService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeService> _logger;
    private readonly IWeatherService _weatherService;
    private readonly string _apiKey;
    private readonly string _model;

    public ClaudeService(HttpClient httpClient, ILogger<ClaudeService> logger, 
        IWeatherService weatherService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _weatherService = weatherService;
        _apiKey = configuration["Claude:ApiKey"] ?? "demo";
        _model = configuration["Claude:Model"] ?? "claude-3-sonnet-20240229";
        
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<string> ChatAsync(string message, string? systemPrompt = null)
    {
        if (_apiKey == "demo")
        {
            return CreateDemoResponse(message);
        }

        try
        {
            _logger.LogInformation("Calling Claude API with model: {Model}", _model);
            
            var request = new
            {
                model = _model,
                max_tokens = 1000,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                system = systemPrompt
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogDebug("Claude API request: {Request}", json);
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Claude API response status: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Claude API response: {Response}", responseText);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: Status {StatusCode}, Response: {Error}", response.StatusCode, responseText);
                return $"Claude API Error ({response.StatusCode}): {responseText}";
            }

            var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var result = claudeResponse?.Content?.FirstOrDefault()?.Text ?? "No response from Claude";
            
            _logger.LogInformation("Claude response received, length: {Length}", result.Length);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Claude API");
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> ChatWithToolsAsync(string message, string userId)
    {
        _logger.LogInformation("ClaudeService.ChatWithToolsAsync: userId={UserId}", userId);
            
        if (_apiKey == "demo")
        {
            return await CreateDemoToolResponse(message, userId);
        }

        try
        {
            _logger.LogInformation("Calling Claude API with tools for user: {UserId}", userId);
            
            var request = new
            {
                model = _model,
                max_tokens = 1000,
                messages = new[]
                {
                    new { role = "user", content = message }
                },
                tools = new[]
                {
                    new
                    {
                        name = "get_weather",
                        description = "Get current weather information for a specific city",
                        input_schema = new
                        {
                            type = "object",
                            properties = new Dictionary<string, object>
                            {
                                ["city"] = new
                                {
                                    type = "string",
                                    description = "The name of the city to get weather for"
                                }
                            },
                            required = new[] { "city" }
                        }
                    }
                },
                system = $"You are a helpful assistant. You can get weather information for cities using the get_weather tool. Always use the tool when asked about weather. The user's ID is {userId}. When providing weather information, always address the user by name and include the temperature in the appropriate unit (Celsius for most countries, Fahrenheit for US). Be conversational and friendly."
            };

            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions { WriteIndented = true });
            _logger.LogDebug("Claude API tools request: {Request}", json);
            
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
            var responseText = await response.Content.ReadAsStringAsync();
            
            _logger.LogDebug("Claude API tools response: {Response}", responseText);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API tools error: Status {StatusCode}, Response: {Error}", response.StatusCode, responseText);
                return $"Claude API Error ({response.StatusCode}): {responseText}";
            }

            var claudeResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            // Handle tool calls if present
            if (claudeResponse?.Content?.Any(c => c.Type == "tool_use") == true)
            {
                _logger.LogInformation("Claude made tool calls, processing...");
                return await HandleToolCalls(claudeResponse, message, userId);
            }

            var textResponse = claudeResponse?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
            return textResponse ?? "No response from Claude";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Claude API with tools");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> HandleToolCalls(ClaudeResponse claudeResponse, string originalMessage, string userId)
    {
        var results = new List<string>();
        var toolMessages = new List<object>();
        
        // Add original user message
        toolMessages.Add(new { role = "user", content = originalMessage });
        
        // Add Claude's response with tool calls
        toolMessages.Add(new { role = "assistant", content = claudeResponse.Content });
        
        foreach (var content in claudeResponse.Content ?? new ClaudeContent[0])
        {
            if (content.Type == "tool_use" && content.Name == "get_weather")
            {
                try
                {
                    _logger.LogInformation("Executing weather tool for tool_use_id: {Id}", content.Id);
                    
                    var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content.Input?.ToString() ?? "{}");
                    if (input?.TryGetValue("city", out var cityElement) == true)
                    {
                        var city = cityElement.GetString() ?? "";
                        _logger.LogInformation("Calling weather service for city: {City}", city);
                        
                        var weather = await _weatherService.GetWeatherAsync(city, userId);
                        if (weather != null)
                        {
                            var weatherResult = new
                            {
                                city = weather.City,
                                country = weather.Country,
                                temperature = weather.Temperature,
                                description = weather.Description,
                                humidity = weather.Humidity,
                                pressure = weather.Pressure,
                                wind_speed = weather.WindSpeed
                            };
                            
                            // Add tool result message
                            toolMessages.Add(new 
                            { 
                                role = "user", 
                                content = new[]
                                {
                                    new
                                    {
                                        type = "tool_result",
                                        tool_use_id = content.Id,
                                        content = JsonSerializer.Serialize(weatherResult)
                                    }
                                }
                            });
                            
                            results.Add($"🌤️ Weather in {weather.City}, {weather.Country}: {weather.Temperature}{weather.TemperatureUnit}, {weather.Description}");
                        }
                        else
                        {
                            toolMessages.Add(new 
                            { 
                                role = "user", 
                                content = new[]
                                {
                                    new
                                    {
                                        type = "tool_result",
                                        tool_use_id = content.Id,
                                        content = "Weather data not available"
                                    }
                                }
                            });
                            results.Add("Weather data not available");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to execute tool call");
                    toolMessages.Add(new 
                    { 
                        role = "user", 
                        content = new[]
                        {
                            new
                            {
                                type = "tool_result",
                                tool_use_id = content.Id,
                                content = "Error getting weather data"
                            }
                        }
                    });
                    results.Add("Failed to get weather data");
                }
            }
        }

        // If we have tool results, call Claude again to get final response
        if (toolMessages.Count > 2)
        {
            try
            {
                var followupRequest = new
                {
                    model = _model,
                    max_tokens = 1000,
                    messages = toolMessages
                };

                var json = JsonSerializer.Serialize(followupRequest, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogDebug("Claude followup request: {Request}", json);
                
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://api.anthropic.com/v1/messages", content);
                var responseText = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var finalResponse = JsonSerializer.Deserialize<ClaudeResponse>(responseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var finalText = finalResponse?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;
                    
                    if (!string.IsNullOrEmpty(finalText))
                    {
                        return finalText;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get followup response from Claude");
            }
        }

        return results.Any() ? string.Join("\n", results) : "Tool execution completed but no results returned";
    }

    private string CreateDemoResponse(string message)
    {
        var responses = new[]
        {
            $"Hello! You asked: '{message}'. This is a demo response from Claude.",
            $"I understand you're asking about: '{message}'. In demo mode, I can provide general assistance.",
            $"Thanks for your message: '{message}'. I'm running in demo mode - connect a real API key for full functionality.",
            $"Your query '{message}' is interesting! This is a simulated Claude response for testing."
        };

        var random = new Random();
        return responses[random.Next(responses.Length)];
    }

    private async Task<string> CreateDemoToolResponse(string message, string userId)
    {
        _logger.LogInformation("ClaudeService.CreateDemoToolResponse: userId={UserId}", userId);
            
        if (message.ToLower().Contains("weather"))
        {
            var cities = new[] { "London", "New York", "Tokyo", "Paris", "Sydney" };
            var random = new Random();
            var city = cities[random.Next(cities.Length)];
            
            var weather = await _weatherService.GetWeatherAsync(city);
            return $"I see you're asking about weather! Here's the current weather for {city}: {weather?.Temperature}°C, {weather?.Description}. (Demo mode)";
        }
        

        return CreateDemoResponse(message);
    }
}

// Response models for Claude API
public class ClaudeResponse
{
    public ClaudeContent[]? Content { get; set; }
}

public class ClaudeContent
{
    public string Type { get; set; } = string.Empty;
    public string? Text { get; set; }
    public string? Name { get; set; }
    public string? Id { get; set; }
    public object? Input { get; set; }
}