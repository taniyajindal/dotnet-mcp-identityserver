using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpAuthServer.Services;

public interface IWeatherService
{
    Task<WeatherData?> GetWeatherAsync(string city, string? userId = null);
}

public class WeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _defaultApiKey;

    public WeatherService(HttpClient httpClient, ILogger<WeatherService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _defaultApiKey = configuration["Weather:ApiKey"] ?? "open-meteo"; // Use Open-Meteo by default
    }

    public async Task<WeatherData?> GetWeatherAsync(string city, string? userId = null)
    {
        try
        {
            var apiKey = GetApiKeyForUser(userId);
            _logger.LogDebug("Using API key for user {UserId}: {ApiKeyType}", userId, apiKey == "demo" ? "demo" : apiKey == "open-meteo" ? "open-meteo" : "custom");

            if (apiKey == "demo")
            {
                return CreateDemoWeather(city);
            }

            // First, get coordinates for the city using Open-Meteo Geocoding API
            var geocodeUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";
            _logger.LogInformation("Geocoding request for city '{City}': {Url}", city, geocodeUrl);
            
            var geocodeResponse = await _httpClient.GetStringAsync(geocodeUrl);
            _logger.LogDebug("Geocoding response: {Response}", geocodeResponse);
            
            var geocodeData = JsonSerializer.Deserialize<OpenMeteoGeocodeResponse>(geocodeResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (geocodeData?.Results == null || geocodeData.Results.Length == 0)
            {
                _logger.LogWarning("City not found or no results: {City}. Response: {Response}", city, geocodeResponse);
                return null;
            }

            var location = geocodeData.Results[0];
            _logger.LogInformation("Found location: {Name}, {Country} at {Lat},{Lon}", location.Name, location.Country, location.Latitude, location.Longitude);

            // Determine temperature unit based on country
            var (tempUnit, tempParam) = GetTemperatureUnit(location.Country);
            _logger.LogDebug("Using temperature unit {Unit} for country {Country}", tempUnit, location.Country);

            // Get weather data using Open-Meteo Weather API with appropriate units
            var weatherUrl = $"https://api.open-meteo.com/v1/forecast?latitude={location.Latitude}&longitude={location.Longitude}&current=temperature_2m,relative_humidity_2m,surface_pressure,wind_speed_10m,cloud_cover&current_units=temperature_2m:{tempParam},relative_humidity_2m:%,surface_pressure:hPa,wind_speed_10m:m/s,cloud_cover:%&timezone=auto";
            _logger.LogInformation("Weather request: {Url}", weatherUrl);
            
            var weatherResponse = await _httpClient.GetStringAsync(weatherUrl);
            _logger.LogDebug("Weather response: {Response}", weatherResponse);
            
            var weatherData = JsonSerializer.Deserialize<OpenMeteoWeatherResponse>(weatherResponse, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (weatherData?.Current != null)
            {
                var result = new WeatherData
                {
                    City = location.Name,
                    Country = location.Country ?? "",
                    Temperature = weatherData.Current.Temperature2m,
                    TemperatureUnit = tempUnit,
                    Description = GetWeatherDescription(weatherData.Current.CloudCover),
                    Humidity = (int)weatherData.Current.RelativeHumidity2m,
                    Pressure = weatherData.Current.SurfacePressure,
                    WindSpeed = weatherData.Current.WindSpeed10m,
                    CloudCover = (int)weatherData.Current.CloudCover
                };
                
                _logger.LogInformation("Weather data retrieved successfully for {City}: {Temp}°C, {Description}", result.City, result.Temperature, result.Description);
                return result;
            }
            else
            {
                _logger.LogWarning("No current weather data in response for {City}. Response: {Response}", city, weatherResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather for {City}", city);
        }

        return null;
    }

    private string GetApiKeyForUser(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogDebug("No user ID provided, using default API key");
            return _defaultApiKey;
        }

        // Try to get user-specific API key from configuration
        var userApiKey = _configuration[$"Weather:UserApiKeys:{userId}"];
        if (!string.IsNullOrEmpty(userApiKey))
        {
            _logger.LogDebug("Found custom API key for user {UserId}", userId);
            return userApiKey;
        }

        // Check for role-based API keys
        var userRoleKey = _configuration[$"Weather:RoleApiKeys:{userId}:Role"];
        if (!string.IsNullOrEmpty(userRoleKey))
        {
            var roleApiKey = _configuration[$"Weather:RoleApiKeys:{userRoleKey}"];
            if (!string.IsNullOrEmpty(roleApiKey))
            {
                _logger.LogDebug("Found role-based API key for user {UserId} with role {Role}", userId, userRoleKey);
                return roleApiKey;
            }
        }

        // Check for premium users
        var isPremiumUser = _configuration.GetValue<bool>($"Weather:PremiumUsers:{userId}");
        if (isPremiumUser)
        {
            var premiumApiKey = _configuration["Weather:PremiumApiKey"];
            if (!string.IsNullOrEmpty(premiumApiKey))
            {
                _logger.LogDebug("Using premium API key for user {UserId}", userId);
                return premiumApiKey;
            }
        }

        // Fallback to default
        _logger.LogDebug("Using default API key for user {UserId}", userId);
        return _defaultApiKey;
    }

    private (string unit, string param) GetTemperatureUnit(string? country)
    {
        // Countries that use Fahrenheit
        var fahrenheitCountries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "United States", "US", "USA", "United States of America",
            "Liberia", "Myanmar", "Burma",
            // US territories
            "Puerto Rico", "Guam", "American Samoa", "US Virgin Islands"
        };

        if (!string.IsNullOrEmpty(country) && fahrenheitCountries.Contains(country))
        {
            return ("°F", "°F");
        }

        // Default to Celsius for rest of the world
        return ("°C", "°C");
    }

    private string GetWeatherDescription(double cloudCover)
    {
        return cloudCover switch
        {
            <= 10 => "Clear sky",
            <= 25 => "Mostly clear",
            <= 50 => "Partly cloudy",
            <= 75 => "Mostly cloudy",
            _ => "Overcast"
        };
    }

    private WeatherData CreateDemoWeather(string city)
    {
        var random = new Random();
        var temperatures = new[] { 18, 22, 25, 28, 15, 12, 30, 8, 35 };
        var descriptions = new[] { "Clear sky", "Partly cloudy", "Overcast", "Light rain", "Sunny", "Cloudy" };
        
        // For demo, assume US cities use Fahrenheit
        var isUSCity = city.ToLower().Contains("new york") || city.ToLower().Contains("los angeles") || 
                       city.ToLower().Contains("chicago") || city.ToLower().Contains("miami") ||
                       city.ToLower().Contains("boston") || city.ToLower().Contains("seattle");
        
        var (tempUnit, _) = isUSCity ? ("°F", "°F") : ("°C", "°C");
        var temp = temperatures[random.Next(temperatures.Length)];
        
        // Convert to Fahrenheit for US cities in demo
        if (isUSCity)
        {
            temp = (int)(temp * 9.0 / 5.0 + 32);
        }
        
        return new WeatherData
        {
            City = city,
            Country = isUSCity ? "United States" : "Demo",
            Temperature = temp,
            TemperatureUnit = tempUnit,
            Description = descriptions[random.Next(descriptions.Length)],
            Humidity = random.Next(30, 80),
            Pressure = random.Next(1000, 1020),
            WindSpeed = random.Next(5, 25),
            CloudCover = random.Next(0, 100)
        };
    }
}

public class WeatherData
{
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public string TemperatureUnit { get; set; } = "°C";
    public string Description { get; set; } = string.Empty;
    public int Humidity { get; set; }
    public double Pressure { get; set; }
    public double WindSpeed { get; set; }
    public int CloudCover { get; set; }
}

// Open-Meteo API response models
public class OpenMeteoGeocodeResponse
{
    public OpenMeteoLocation[]? Results { get; set; }
}

public class OpenMeteoLocation
{
    public string Name { get; set; } = string.Empty;
    public string? Country { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class OpenMeteoWeatherResponse
{
    public OpenMeteoCurrentWeather? Current { get; set; }
}

public class OpenMeteoCurrentWeather
{
    [JsonPropertyName("temperature_2m")]
    public double Temperature2m { get; set; }
    
    [JsonPropertyName("relative_humidity_2m")]
    public double RelativeHumidity2m { get; set; }
    
    [JsonPropertyName("surface_pressure")]
    public double SurfacePressure { get; set; }
    
    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed10m { get; set; }
    
    [JsonPropertyName("cloud_cover")]
    public double CloudCover { get; set; }
}