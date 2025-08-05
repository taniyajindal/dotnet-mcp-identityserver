using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient<McpAuthServer.Services.IWeatherService, McpAuthServer.Services.WeatherService>();
builder.Services.AddHttpClient<McpAuthServer.Services.IClaudeService, McpAuthServer.Services.ClaudeService>();
builder.Services.AddSingleton<McpAuthServer.Services.IUserApiKeyService, McpAuthServer.Services.UserApiKeyService>();

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var identityConfig = builder.Configuration.GetSection("IdentityServer");
        
        options.Authority = identityConfig["Authority"];
        options.Audience = identityConfig["Audience"];
        options.RequireHttpsMetadata = identityConfig.GetValue<bool>("RequireHttpsMetadata", true);
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false, // Disable signing key validation due to expired cert
            RequireSignedTokens = false, // Allow for testing with expired cert
            ClockSkew = TimeSpan.FromMinutes(5)
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                Console.WriteLine($"Exception details: {context.Exception}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                Console.WriteLine($"Token validated for user: {identity?.Name}");
                Console.WriteLine($"Token issuer: {context.SecurityToken}");
                return Task.CompletedTask;
            },
            OnMessageReceived = context =>
            {
                Console.WriteLine($"Token received: {context.Token?.Substring(0, Math.Min(50, context.Token?.Length ?? 0))}...");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable static file serving
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Serve the web client at root
app.MapFallbackToFile("index.html");

app.Run();