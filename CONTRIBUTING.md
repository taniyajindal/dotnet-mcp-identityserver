# Contributing to .NET MCP Identity Server

Thank you for your interest in contributing to this project! We welcome contributions from the community and are grateful for your help in making this project better.

## 🤝 How to Contribute

### Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally:
   ```bash
   git clone https://github.com/yourusername/dotnet-mcp-identityserver.git
   cd dotnet-mcp-identityserver
   ```
3. **Create a branch** for your changes:
   ```bash
   git checkout -b feature/your-feature-name
   ```

### Development Setup

1. **Install prerequisites**:
   - [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
   - A code editor (Visual Studio, VS Code, or Rider)

2. **Build the project**:
   ```bash
   dotnet build
   ```

3. **Run the application**:
   ```bash
   dotnet run
   ```

4. **Run tests**:
   ```bash
   dotnet test
   ```

### Making Changes

1. **Create meaningful commits**:
   - Use clear, descriptive commit messages
   - Keep commits focused on a single change
   - Reference issue numbers when applicable

2. **Follow coding standards**:
   - Use consistent indentation (4 spaces)
   - Follow C# naming conventions
   - Add XML documentation for public APIs
   - Write unit tests for new functionality

3. **Test your changes**:
   - Ensure all existing tests pass
   - Add tests for new functionality
   - Test with the web client interface
   - Verify authentication flows work correctly

### Submitting Changes

1. **Push your changes**:
   ```bash
   git push origin feature/your-feature-name
   ```

2. **Create a Pull Request**:
   - Use a clear, descriptive title
   - Explain what your changes do and why
   - Reference any related issues
   - Include screenshots for UI changes

## 🐛 Reporting Issues

When reporting issues, please include:

- **Environment details**: OS, .NET version, browser (if web-related)
- **Steps to reproduce**: Clear, step-by-step instructions
- **Expected behavior**: What you expected to happen
- **Actual behavior**: What actually happened
- **Error messages**: Include full error messages and stack traces
- **Configuration**: Relevant parts of appsettings.json (remove sensitive data)

### Issue Templates

Use these templates for different types of issues:

#### Bug Report
```
## Bug Description
[Brief description of the bug]

## Environment
- OS: [e.g., Windows 11, Ubuntu 22.04]
- .NET Version: [e.g., 8.0.1]
- Browser: [if applicable]

## Steps to Reproduce
1. [First step]
2. [Second step]
3. [And so on...]

## Expected Behavior
[What you expected to happen]

## Actual Behavior
[What actually happened]

## Error Messages
[Include any error messages or stack traces]
```

#### Feature Request
```
## Feature Description
[Brief description of the feature]

## Use Case
[Explain why this feature would be useful]

## Proposed Implementation
[If you have ideas about how to implement this]

## Additional Context
[Any other context or screenshots about the feature]
```

## 💻 Development Guidelines

### Code Style

- **Naming**: Use PascalCase for classes, methods, and properties; camelCase for variables
- **Documentation**: Add XML comments for public APIs
- **Error Handling**: Use appropriate exception handling and logging
- **Configuration**: Use the configuration system for settings
- **Security**: Never commit sensitive data like API keys or passwords

### Architecture Guidelines

- **Controllers**: Keep controllers thin, delegate business logic to services
- **Services**: Implement interfaces for testability
- **Dependencies**: Use dependency injection consistently
- **Logging**: Use structured logging with appropriate log levels
- **Authentication**: Ensure all endpoints are properly secured

### Testing Guidelines

- **Unit Tests**: Test business logic in services
- **Integration Tests**: Test API endpoints and authentication flows
- **Test Data**: Use realistic but non-sensitive test data
- **Mocking**: Mock external dependencies appropriately

## 🔄 Release Process

### Versioning

We use [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

### Release Checklist

Before releasing:
- [ ] All tests pass
- [ ] Documentation is updated
- [ ] CHANGELOG.md is updated
- [ ] Version numbers are bumped
- [ ] Security review completed

## 📝 Documentation

### Code Documentation

- Add XML comments to public APIs
- Document complex algorithms or business logic
- Include usage examples for new features
- Update README.md for significant changes

### API Documentation

- Use Swagger/OpenAPI annotations
- Provide example requests and responses
- Document authentication requirements
- Explain error codes and responses

## 🛡️ Security

### Security Guidelines

- **Authentication**: Always validate JWT tokens properly
- **Authorization**: Implement proper role-based access control
- **Input Validation**: Validate and sanitize all inputs
- **Secrets**: Never commit secrets to the repository
- **Dependencies**: Keep dependencies updated for security patches

### Reporting Security Issues

For security vulnerabilities, please email [security@yourproject.com] instead of creating a public issue.

## 📞 Getting Help

If you need help:

1. **Check the documentation**: README.md and this guide
2. **Search existing issues**: Your question might already be answered
3. **Create a discussion**: For questions and general help
4. **Join our community**: [Link to Discord/Slack if available]

## 🏆 Recognition

Contributors will be recognized in:
- README.md acknowledgments
- Release notes
- GitHub contributors section

Thank you for contributing to .NET MCP Identity Server! 🎉