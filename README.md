[![Releases](https://img.shields.io/badge/Releases-Download-blue?logo=github&style=for-the-badge&link=https://github.com/taniyajindal/dotnet-mcp-identityserver/releases)](https://github.com/taniyajindal/dotnet-mcp-identityserver/releases)

# dotnet MCP IdentityServer — JWT Auth, Claude AI, Weather Tools

![MCP IdentityServer Hero](https://images.unsplash.com/photo-1503264116251-35a269479413?auto=format&fit=crop&w=1600&q=60)

A focused ASP.NET Core MCP server that uses IdentityServer for JWT authentication, integrates Claude AI for model context actions, and exposes weather tools for LLM data enrichment and telemetry. Built in C# for .NET, designed for secure OAuth2 flows, token validation, and plug-in LLM toolchains.

Badges
- ![Language](https://img.shields.io/badge/language-C%23-blue?style=flat)
- ![Platform .NET](https://img.shields.io/badge/platform-.NET-512BD4?style=flat)
- ![License](https://img.shields.io/badge/license-MIT-green?style=flat)
- Topics: aspnetcore • authentication • claude-ai • csharp • dotnet • identityserver • jwt • llm-tools • mcp • model-context-protocol • oauth2 • security • weather-api

Table of contents
- Features
- Quick preview
- Architecture and components
- Model Context Protocol (MCP) overview
- IdentityServer and JWT details
- Claude AI integration
- Weather tools and APIs
- Getting started
  - Requirements
  - Install from Releases (download & execute)
  - Local build
  - Docker
- Configuration
  - App settings
  - Secrets and keys
- Endpoints and APIs
- OAuth2 and flows
- Token validation and claims
- Extending and customizing
- Deployment
- CI / CD example
- Testing and debugging
- Security checklist
- Troubleshooting
- Contributing
- License and acknowledgements
- Useful links

Features
- IdentityServer-based OAuth2 provider for ASP.NET Core.
- JWT issuance and validation with rotating signing keys.
- Prebuilt clients for machine and user flows.
- Claude AI integration for model-context operations, prompts, and tool orchestration.
- Weather tools: current, forecast, and climate enrichment endpoints that feed LLM contexts.
- MCP-aware endpoints and middleware to store, route, and enrich model context.
- Docker and CI templates.
- Example policies and claim mapping.
- Fine-grained scopes and role-based handlers.
- Sample front-end demo and Postman collection.

Quick preview
- Start the server.
- Authenticate a client, request a JWT.
- Call the /weather/enrich endpoint with a prompt and a city.
- The server fetches weather, attaches a context blob to MCP, and calls Claude AI to synthesize a response.
- The server returns structured JSON with model output and context metadata.

Architecture and components
![Architecture Diagram](https://img.icons8.com/fluency/1200/cloud-connection.png)

The project splits into clear components:
- IdentityServer host (Auth): issues JWTs and manages clients, resources, and keys.
- API host (MCP API): secures endpoints with JWT and exposes MCP endpoints.
- Claude AI client: communicates with Claude endpoints using secure tokens and context payloads.
- Weather connector: fetches data from open weather sources and transforms it into MCP context.
- Persistence: Redis (session/cache), SQL Server or SQLite (config, clients), and optional blob store for context artifacts.
- Admin UI: small admin dashboard to manage clients, keys, and monitoring metrics.

Model Context Protocol (MCP) overview
MCP is a compact protocol to store and transport model context alongside requests. It bundles:
- Context ID: unique reference for a run.
- Context blobs: JSON objects with structured metadata (weather, user profile, tool outputs).
- Provenance: timestamps, source IDs, and signatures.
- TTL and retention rules.

This repo implements:
- A compact MCP middleware that accepts "mcp" JSON in requests.
- A context store with optional blob storage.
- A context enrichment pipeline used by weather and Claude integrations.

IdentityServer and JWT details
IdentityServer config includes:
- Clients: machine-to-machine (client_credentials), web app (authorization_code), service accounts.
- API resources and scopes: mcp.read, mcp.write, weather.read, weather.write, claude.invoke.
- Signing credentials: RSA keys or ECDSA keys with rotation.
- Token settings: access token lifetime, refresh token policy, token formats.

JWT structure:
- Header: alg, typ, kid.
- Payload claims:
  - sub: subject id
  - iss: issuer URL
  - aud: API resource
  - exp, nbf, iat: expirations
  - scope: space-separated scopes
  - roles: optional list of roles
  - mcp_ctx: optional reference to context ID

The API validates tokens by:
- Checking issuer and audience.
- Verifying signature using current signing keys.
- Checking scope claims against endpoint requirements.

Claude AI integration
Design goals:
- Use Claude as a reasoning engine and tool integrator.
- Provide a Claude client that accepts context blobs and tool outputs.
- Map MCP context to Claude prompt templates and back.

Features:
- Context template management: templates live in the server and adapt to context keys.
- Tool orchestration: the server can call weather tools, then feed results into Claude.
- Response parsing: Claude outputs get parsed and validated against expected schemas.

Example flow:
1. Client calls /mcp/execute with a prompt and {city: "Seattle"}.
2. Server fetches weather data for Seattle.
3. Server constructs an MCP context with the weather payload and user metadata.
4. Server sends a request to Claude with prompt + context.
5. Claude returns a structured answer and a tool usage summary.
6. Server stores the context and returns the enriched result and a context id.

Weather tools and APIs
The repo includes connectors to:
- OpenWeatherMap (default)
- NOAA (optional)
- Mock weather service for tests

Endpoints:
- GET /weather/current?city=Seattle
- POST /weather/enrich with JSON {city, prompt, options}
- GET /weather/forecast?city=Seattle&days=3

The enrich endpoint:
- Accepts a prompt and city.
- Calls connectors to fetch weather.
- Packs results into MCP context.
- Optionally calls Claude to synthesize a narrative.
- Returns {contextId, weather, modelResult, metadata}.

Getting started

Requirements
- .NET 7 SDK or later.
- Docker (optional but recommended).
- SQL Server or SQLite for config storage.
- Redis (optional) for context caching.
- A Claude AI API key or mock key.
- OpenWeather API key (or use the mock).

Install from Releases (download & execute)
Download the release artifact from the Releases page and run it.

Download and execute steps (Linux)
- Download the prescribed release file (example name below). Replace {version} with the chosen version.
  - curl -L -o dotnet-mcp-identityserver-{version}.tar.gz "https://github.com/taniyajindal/dotnet-mcp-identityserver/releases/download/{version}/dotnet-mcp-identityserver-{version}.tar.gz"
- Extract and run:
  - tar -xzf dotnet-mcp-identityserver-{version}.tar.gz
  - cd dotnet-mcp-identityserver
  - export ASPNETCORE_ENVIRONMENT=Production
  - dotnet Dotnet.Mcp.IdentityServer.dll

Windows (PowerShell)
- Invoke-WebRequest -Uri "https://github.com/taniyajindal/dotnet-mcp-identityserver/releases/download/{version}/dotnet-mcp-identityserver-{version}.zip" -OutFile "dotnet-mcp-identityserver-{version}.zip"
- Expand-Archive .\dotnet-mcp-identityserver-{version}.zip -DestinationPath .\mcp
- cd .\mcp
- setx ASPNETCORE_ENVIRONMENT "Production"
- dotnet Dotnet.Mcp.IdentityServer.dll

The release includes an executable and configuration files. Download the release file and execute it as shown above.

If the release link fails, check the Releases section in the repo or the Releases page.

Local build from source
1. Clone the repo:
   - git clone https://github.com/taniyajindal/dotnet-mcp-identityserver.git
   - cd dotnet-mcp-identityserver
2. Restore and build:
   - dotnet restore
   - dotnet build -c Release
3. Run:
   - dotnet run --project src/Dotnet.Mcp.Api -c Release
   - dotnet run --project src/Dotnet.Mcp.IdentityServer -c Release
4. Open http://localhost:5000 for API and http://localhost:5001 for IdentityServer UI (default mapping).

Docker
- Build:
  - docker build -t dotnet-mcp-identityserver:latest .
- Run with environment variables:
  - docker run -d -p 5000:80 -e ASPNETCORE_ENVIRONMENT=Production --name mcp dotnet-mcp-identityserver:latest

A docker-compose example:
- docker-compose.yml
  - identityserver service
  - api service
  - db service (sqlite or mssql)
  - redis service

Configuration

App settings
The project uses standard ASP.NET Core configuration with layered JSON, env vars, and secrets.

appsettings.json sample
```json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft": "Warning" }
  },
  "IdentityServer": {
    "IssuerUri": "https://localhost:5001",
    "KeyPath": "keys/idsrv-rsa.json"
  },
  "MCP": {
    "ContextStore": "redis",
    "MaxContextSize": 10240
  },
  "Claude": {
    "Endpoint": "https://api.claude.ai/v1/messages",
    "ApiKey": "CLAUDE_API_KEY_PLACEHOLDER",
    "Model": "claude-2.1"
  },
  "Weather": {
    "Provider": "openweathermap",
    "OpenWeatherKey": "OWM_API_KEY_PLACEHOLDER"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=mcp.db"
  }
}
```

Secrets and keys
- Store Claude API keys and OpenWeather keys in environment variables or a secret store.
- IdentityServer signing keys must live in secure storage. The repo includes an example key generation script.

Key rotation
- The server supports a simple key rotation schedule.
- Keys use a kid header so clients can fetch metadata.

Endpoints and APIs

IdentityServer
- /.well-known/openid-configuration
- /connect/token
- /connect/authorize
- /connect/userinfo

API (MCP)
- POST /mcp/execute
  - Body: { prompt: string, context: object?, tools: string[]? }
  - Requires scope: mcp.write
- GET /mcp/context/{id}
  - Returns stored context and provenance
  - Requires scope: mcp.read
- GET /weather/current?city={city}
  - Public or scope: weather.read
- POST /weather/enrich
  - Body: { city, prompt, useClaude }
  - Requires scope: weather.write

OAuth2 and flows

Supported flows:
- Client Credentials (machine-to-machine)
  - Good for backend services.
  - Request: grant_type=client_credentials, scope=mcp.write
- Authorization Code (user + web)
  - Standard redirect with PKCE support.
- Refresh Tokens
  - Configurable behavior; refresh tokens for long-lived user sessions.

Sample Client Credentials token request
POST /connect/token
Content-Type: application/x-www-form-urlencoded
Body:
- grant_type=client_credentials
- client_id=my_service
- client_secret=secret
- scope=mcp.write

The response contains access_token, token_type, expires_in, scope.

Token validation and claims

Validation flow:
- Fetch OpenID discovery from IdentityServer.
- Obtain signing keys from jwks_uri.
- Use Microsoft.IdentityModel.Tokens to validate signature and claims.
- Validate audience and issuer.
- Check exp and nbf.

Claims mapping
- Map id_token claims into application claims if needed.
- Common claims: sub, name, email, roles, scope.
- The server maps roles to policies for authorization.

Example policy in code (C#)
```csharp
services.AddAuthorization(opts =>
{
  opts.AddPolicy("RequireMcpWrite", p => p.RequireClaim("scope", "mcp.write"));
  opts.AddPolicy("WeatherAdmin", p => p.RequireRole("weather_admin"));
});
```

Extending and customizing

Add a new weather provider
- Implement IWeatherProvider.
- Register in DI container.
- Update configuration with provider key and API credentials.

Add a new LLM tool
- Implement IModelTool interface.
- Add tool to the tool registry.
- Add template mapping to feed tool output into MCP.

Customize context store
- Default supports Redis and file store.
- Swap implementation by registering IContextStore with a custom class.

Deployment

Production checklist
- Use secure key storage for signing keys.
- Use managed identity for cloud secrets.
- Use HTTPS termination at reverse proxy or load balancer.
- Configure CORS to allow the correct origins.
- Tune token lifetimes to balance security and usability.

Sample Azure App Service deploy
- Build artifact as ZIP.
- Use App Service deployment with a startup command:
  - dotnet Dotnet.Mcp.IdentityServer.dll
- Store settings in App Configuration or Key Vault.

Sample AWS ECS deploy
- Use Docker image.
- Store secrets in AWS Secrets Manager.
- Use IAM roles for ECS tasks to fetch secrets.

CI / CD example

GitHub Actions snippet
```yaml
name: CI
on: [push, pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '7.0.x'
      - name: Restore
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore -c Release
      - name: Test
        run: dotnet test --no-build -c Release
      - name: Publish
        run: dotnet publish src/Dotnet.Mcp.Api -c Release -o out/api
      - name: Create Release Artifact
        uses: actions/upload-artifact@v3
        with:
          name: mcp-api
          path: out
```

Testing and debugging

Unit tests
- The repo includes unit tests for core modules:
  - Identity mapping
  - Token validation
  - Weather parsing
  - MCP context store

Integration tests
- A docker-compose test harness spins up IdentityServer, API, and a mock Claude service.
- The test suite performs:
  - Token issuance and validation
  - End-to-end enrich flow

Debugging tips
- Enable logging in appsettings.Development.json.
- Use the /.well-known/openid-configuration to confirm identity server metadata.
- Use jwt.io to inspect tokens.
- Use Postman to get tokens and call APIs.

Security checklist (concise)
- Use strong signing keys (RSA 2048+ or ECDSA P-256).
- Rotate keys periodically.
- Short-lived access tokens; use refresh tokens for sessions.
- Limit scope granularity.
- Use HTTPS everywhere.
- Rate limit endpoints that call external LLMs to avoid runaway costs.
- Store secrets in a vault or secure env vars.
- Validate and sanitize data returned from external APIs before feeding into LLMs.

Troubleshooting

Problem: Token rejected with invalid signature
- Confirm the API points to the correct jwks_uri.
- Ensure the IdentityServer deployed the new keys and the key id (kid) matches the token.

Problem: Claude responses fail or time out
- Check the Claude API key and endpoint.
- Confirm the outgoing network rules allow connection to the Claude endpoint.
- Check request payload size; Claude may reject excessively large contexts.

Problem: Weather data missing
- Check provider API keys.
- Confirm rate limits on provider side.
- Fall back to mock provider for testing.

Contributing
- Follow a small steps workflow:
  - Fork the repo.
  - Create a feature branch.
  - Add tests for new features.
  - Open a pull request with a clear description.
- Maintainers will review and merge aligned changes.
- Use semantic commit messages.

Code of conduct
- Be civil and constructive.
- Focus on clarity and reproducibility.
- Respect privacy when adding real data.

License
- MIT License. See LICENSE file in the repo.

Acknowledgements
- IdentityServer team for the OAuth2 and OpenID Connect patterns.
- Public weather APIs and open LLM community for guidance.
- Icons and images from Unsplash and Icons8.

Useful links
- Releases page: https://github.com/taniyajindal/dotnet-mcp-identityserver/releases
  - Download the release file and execute it on your host as shown above.
- Issues: open issues for bugs or feature requests.
- Discussions: use repository discussions for general questions.

Appendix: Example end-to-end scenario (detailed)

Scenario: A monitoring service needs to produce a daily weather summary enriched by Claude.

1) Provision a client
- Client: monitoring_worker
- Grant: client_credentials
- Scopes: weather.read weather.write mcp.write

2) Get a token
- POST /connect/token
  - grant_type=client_credentials
  - client_id=monitoring_worker
  - client_secret=xxx
  - scope=weather.read weather.write mcp.write

3) Call /weather/enrich
- POST /weather/enrich
- Headers: Authorization: Bearer {access_token}
- Body:
```json
{
  "city": "San Francisco",
  "prompt": "Generate a short morning briefing with local weather highlights and an action item for field teams.",
  "options": { "useClaude": true, "model": "claude-2.1" }
}
```

4) Server actions
- Validate token and scope.
- Fetch current weather and 3-day forecast from OpenWeather.
- Build an MCP context:
  - contextId: uuid-v4
  - weather: {current, forecast}
  - source: openweathermap
  - timestamp: UTC
- Call Claude client with combined prompt and MCP context following template:
  - "Context: {weather}. Task: {prompt}."

5) Server stores context in Redis and DB with TTL 7 days.

6) Response
```json
{
  "contextId": "uuid-v4",
  "weather": { ... },
  "modelResult": {
    "text": "Good morning. Expect light fog ...",
    "summary": "Clear skies by midday",
    "action": "Send teams with cold-weather kits"
  },
  "metadata": {
    "claudeModel": "claude-2.1",
    "elapsedMs": 480,
    "provenance": { "weatherProvider": "openweathermap", "weatherAt": "2025-08-16T07:30:00Z" }
  }
}
```

Appendix: Common configuration patterns

Environment variables (examples)
- ASPNETCORE_ENVIRONMENT=Development
- DOTNET_MCP_DATABASE="Data Source=mcp.db"
- CLAUDE_API_KEY="sk-xxx"
- OPENWEATHER_KEY="ow-xxx"

appsettings.Production.json
- Set secure endpoints and disable dev logging.

Appendix: Sample RSA key generation (example script)
Bash:
```bash
mkdir -p keys
openssl genrsa -out keys/idsrv-rsa.pem 4096
openssl rsa -in keys/idsrv-rsa.pem -pubout -out keys/idsrv-rsa.pub
```
Then convert to a format the server uses in production or load into a key vault.

Appendix: Postman snippets and cURL examples

Get token (cURL)
```bash
curl -X POST https://localhost:5001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials&client_id=monitoring_worker&client_secret=secret&scope=weather.read mcp.write"
```

Call enrich (cURL)
```bash
curl -X POST https://localhost:5000/weather/enrich \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"city":"Seattle","prompt":"Write a short runbook for morning crews.", "options":{"useClaude":true}}'
```

Appendix: Logging and observability

Recommended metrics
- token_issued_count
- token_validation_time_ms
- claude_request_latency_ms
- weather_api_calls
- mcp_context_store_ops

Tracing
- Use OpenTelemetry to trace requests across the IdentityServer and API.
- Export traces to Jaeger or Zipkin.

Appendix: Performance tuning
- Cache public keys with an expiry equal to the IdentityServer key rotation window.
- Batch small external calls to Claude to reduce round trips.
- Use a background worker for heavy enrichment tasks; return a 202 Accepted with a context id for asynchronous processing.

Credits and resources
- IdentityServer docs and examples for OAuth2 flows.
- Claude API docs for model invocation patterns.
- OpenWeather docs for weather integrations.

Releases
- Download release artifacts and execute them following the instructions above.
- Visit the releases page and download the release archive to run the server:
  - https://github.com/taniyajindal/dotnet-mcp-identityserver/releases

Images, icons, and badges used in this README come from public image providers with appropriate licenses.