using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using McpAuthServer.Services;

namespace McpAuthServer.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous] // Allow testing without auth
public class WeatherTestController : ControllerBase
{
    private readonly IWeatherService _weatherService;

    public WeatherTestController(IWeatherService weatherService)
    {
        _weatherService = weatherService;
    }

    [HttpGet("{city}")]
    public async Task<IActionResult> GetWeather(string city)
    {
        try
        {
            var userId = "test-user"; // For testing
        var weather = await _weatherService.GetWeatherAsync(city, userId);
            
            if (weather == null)
            {
                return NotFound(new { error = $"Weather data not available for {city}" });
            }

            return Ok(weather);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("test/direct/{city}")]
    public async Task<IActionResult> TestDirectAPI(string city)
    {
        try
        {
            var httpClient = new HttpClient();
            
            // Test geocoding
            var geocodeUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";
            var geocodeResponse = await httpClient.GetStringAsync(geocodeUrl);
            
            // Test weather (using London coordinates as example)
            var weatherUrl = "https://api.open-meteo.com/v1/forecast?latitude=51.5074&longitude=-0.1278&current=temperature_2m,relative_humidity_2m,surface_pressure,wind_speed_10m,cloud_cover&timezone=auto";
            var weatherResponse = await httpClient.GetStringAsync(weatherUrl);
            
            return Ok(new
            {
                city,
                geocodeUrl,
                geocodeResponse,
                weatherUrl,
                weatherResponse
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message, stack = ex.StackTrace });
        }
    }
}