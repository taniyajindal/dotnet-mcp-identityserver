# .NET MCP Identity Server

An ASP.NET Core implementation of the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/) server with Identity Server JWT authentication, featuring weather tools and Claude AI integration.

> ⚠️ **Note**: This is a demonstration/educational project. Please thoroughly test and review the code before using in any production environment.

## ✨ Features

- **🔐 Identity Server Integration**: Full JWT authentication with configurable Identity Server
- **🌤️ Weather Tools**: Open-Meteo API integration with location-based temperature units
- **🤖 Claude AI Integration**: Tool-enabled conversations with Anthropic's Claude
- **👤 User Management**: Comprehensive user claims and API key management
- **🌐 Web Interface**: Built-in test client for easy development and testing
- **📡 MCP Protocol**: Complete Model Context Protocol implementation
- **🔧 Configurable**: Environment-based configuration for different deployments

## 🚀 Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Identity Server instance (for authentication)
- Claude API key (optional, demo mode available)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/dotnet-mcp-identityserver.git
   cd dotnet-mcp-identityserver
   ```

2. **Configure settings**
   ```bash
   cp appsettings.json appsettings.Development.json
   # Edit appsettings.Development.json with your settings
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the web interface**
   Open https://localhost:7000 in your browser

## ⚙️ Configuration

### Identity Server Setup

Update `appsettings.json` with your Identity Server configuration:

```json
{
  "IdentityServer": {
    "Authority": "https://your-identity-server.com",
    "Audience": "YOUR_CLIENT_ID",
    "RequireHttpsMetadata": true
  }
}
```

### API Keys Configuration

```json
{
  "Claude": {
    "ApiKey": "your-claude-api-key",
    "Model": "claude-3-sonnet-20240229"
  },
  "Weather": {
    "ApiKeys": {
      "user1": "user-specific-key",
      "premium": "premium-api-key"
    }
  }
}
```

## 🔧 Usage

### Authentication

The server requires JWT Bearer tokens from your Identity Server. Include the token in the Authorization header:

```bash
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     https://localhost:7000/api/mcp/tools
```

### Available Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/mcp/tools` | GET | List available MCP tools |
| `/api/mcp/tools/call` | POST | Execute MCP tools |
| `/api/mcp/resources` | GET | List available resources |
| `/api/chat/completions` | POST | Chat with Claude (with/without tools) |
| `/api/user/details` | GET | Get user information and claims |

### MCP Tools

#### Weather Tool
```json
{
  "name": "get_weather",
  "arguments": {
    "city": "London"
  }
}
```

#### Claude Chat Tool
```json
{
  "name": "chat_with_claude",
  "arguments": {
    "message": "Hello, how are you?"
  }
}
```

### User Claims

The server extracts and uses various claims from JWT tokens:

- `sub`: User ID
- `name`: Display name
- `email`: Email address
- `role`: User role
- `companyId`: Company identifier
- `firstName`, `lastName`: User names

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Web Client    │    │   Identity       │    │   External      │
│   (Test UI)     │    │   Server         │    │   APIs          │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         ├─── HTTP/JWT ──────────┼──── JWT Validation ───┤
         │                       │                       │
┌─────────────────────────────────────────────────────────────────┐
│                    MCP Auth Server                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐           │
│  │   MCP       │  │   Chat      │  │   User      │           │
│  │ Controller  │  │ Controller  │  │ Controller  │           │
│  └─────────────┘  └─────────────┘  └─────────────┘           │
│         │                 │                 │                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐           │
│  │  Weather    │  │   Claude    │  │  UserAPI    │           │
│  │  Service    │  │  Service    │  │ KeyService  │           │
│  └─────────────┘  └─────────────┘  └─────────────┘           │
└─────────────────────────────────────────────────────────────────┘
```

## 🧪 Development

### Project Structure

```
├── Controllers/           # API controllers
│   ├── McpController.cs  # MCP protocol endpoints
│   ├── ChatController.cs # Claude AI chat endpoints
│   └── UserController.cs # User management endpoints
├── Services/             # Business logic services
│   ├── ClaudeService.cs  # Claude AI integration
│   ├── WeatherService.cs # Weather API integration
│   └── UserApiKeyService.cs # API key management
├── wwwroot/              # Static web files
│   ├── index.html        # Test client interface
│   └── mcp-client.js     # Client-side JavaScript
├── appsettings.json      # Configuration
└── Program.cs            # Application startup
```

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Building for Production

```bash
# Build optimized release
dotnet publish -c Release -o ./publish

# Or use Docker
docker build -t mcp-auth-server .
```

## 🐳 Docker Support

Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["McpAuthServer.csproj", "."]
RUN dotnet restore "McpAuthServer.csproj"
COPY . .
RUN dotnet build "McpAuthServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "McpAuthServer.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "McpAuthServer.dll"]
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [Model Context Protocol](https://modelcontextprotocol.io/) by Anthropic
- [Identity Server](https://identityserver.io/) for authentication
- [Open-Meteo](https://open-meteo.com/) for weather data
- [Anthropic Claude](https://www.anthropic.com/) for AI capabilities

## 📚 Additional Resources

- [MCP Protocol Specification](https://spec.modelcontextprotocol.io/)
- [Identity Server Documentation](https://identityserver4.readthedocs.io/)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)

---

⭐ **Star this repository** if you find it helpful!