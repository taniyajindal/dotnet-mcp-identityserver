namespace McpAuthServer.Services;

public interface IUserApiKeyService
{
    Task<string?> GetWeatherApiKeyAsync(string userId);
    Task SetWeatherApiKeyAsync(string userId, string apiKey);
    Task<bool> IsUserPremiumAsync(string userId);
    Task<string?> GetUserRoleAsync(string userId);
}

public class UserApiKeyService : IUserApiKeyService
{
    private readonly ILogger<UserApiKeyService> _logger;
    private readonly IConfiguration _configuration;
    // In a real implementation, you'd inject a database context here

    public UserApiKeyService(ILogger<UserApiKeyService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string?> GetWeatherApiKeyAsync(string userId)
    {
        try
        {
            // In a real implementation, this would query a database
            // For now, check configuration
            var userApiKey = _configuration[$"Weather:UserApiKeys:{userId}"];
            if (!string.IsNullOrEmpty(userApiKey))
            {
                _logger.LogDebug("Found weather API key for user {UserId}", userId);
                return userApiKey;
            }

            // Check if user is premium
            if (await IsUserPremiumAsync(userId))
            {
                var premiumKey = _configuration["Weather:PremiumApiKey"];
                if (!string.IsNullOrEmpty(premiumKey))
                {
                    _logger.LogDebug("Using premium weather API key for user {UserId}", userId);
                    return premiumKey;
                }
            }

            // Check role-based keys
            var userRole = await GetUserRoleAsync(userId);
            if (!string.IsNullOrEmpty(userRole))
            {
                var roleKey = _configuration[$"Weather:RoleApiKeys:{userRole}"];
                if (!string.IsNullOrEmpty(roleKey))
                {
                    _logger.LogDebug("Using role-based weather API key for user {UserId} with role {Role}", userId, userRole);
                    return roleKey;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting weather API key for user {UserId}", userId);
            return null;
        }
    }

    public async Task SetWeatherApiKeyAsync(string userId, string apiKey)
    {
        try
        {
            // In a real implementation, this would save to database
            _logger.LogInformation("Setting weather API key for user {UserId}", userId);
            
            // For now, you could save to a local cache or database
            // This is just a placeholder for the interface
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting weather API key for user {UserId}", userId);
        }
    }

    public Task<bool> IsUserPremiumAsync(string userId)
    {
        try
        {
            // Check configuration for premium users
            var isPremium = _configuration.GetValue<bool>($"Weather:PremiumUsers:{userId}");
            
            // In a real implementation, check database:
            // var user = await _dbContext.Users.FindAsync(userId);
            // return user?.IsPremium == true;
            
            return Task.FromResult(isPremium);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking premium status for user {UserId}", userId);
            return Task.FromResult(false);
        }
    }

    public async Task<string?> GetUserRoleAsync(string userId)
    {
        try
        {
            // In a real implementation, get from database or claims
            // For now, return a placeholder
            
            // You could also get this from JWT claims in the controller
            // var role = HttpContext.User.FindFirst("role")?.Value;
            
            await Task.CompletedTask;
            return null; // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role for user {UserId}", userId);
            return null;
        }
    }
}