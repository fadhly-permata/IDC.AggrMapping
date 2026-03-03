# Copilot Guidelines for IDC.Template Project

## Project Architecture Overview
This is a .NET 8.0 web API template with modular dependency injection, configuration-driven middleware, and extensive customization features.

### Key Components
- **Configuration Handlers**: `AppConfigsHandler` and `AppSettingsHandler` manage JSON configurations with dot notation paths (`_config.PropGet("Security.Cors.Enabled")`)
- **Partial Program Classes**: Main setup split across `Program.cs`, `Program.DI.cs`, `Program.Middlewares.cs`, `Program.Services.cs`, etc.
- **Controller Pattern**: Split controllers by feature (e.g., `DemoController.Cache.cs`, `DemoController.ApiKey.cs`)
- **IDC.Utilities Dependency**: External utility library referenced as local DLL at `D:\- Works\SCM\idc.utility\bin\Release\net8.0\IDC.Utilities.dll`

### Configuration Files
- `appsettings.json`: Standard ASP.NET settings, used by `AppSettingsHandler`  
- `endpoint_generator.jsonc`: Dynamic API endpoint generation definitions

## Coding Standards
- Use C# code formatting for files with the .cs extension.
- Always add argument names when calling methods: `config.Get(path: "app.name", defaultValue: "default")`
- Implement nullable and null safety throughout
- Use simplified collection initialization and collection expressions
- Functions should return the class type to enable method chaining
- Controllers use primary constructors: `public class DemoController(SystemLogging systemLogging, Language language)`
- Do not use curly braces "{}" for single-line statements.
- Variable declaration is unnecessary if the value is used only once; use the value directly.

## Documentation Standards
- Use English with formal tone for XML documentation
- Include comprehensive sections: Summary, Sample Code, Parameters, Returns, Exceptions
- Generate documentation for private/internal methods too
- Use DocFX-compatible XML with `<code>` tags
- Limit lines to 100 characters maximum
- Use DocFX alerts: `> [!NOTE]`, `> [!TIP]`, `> [!IMPORTANT]`, `> [!CAUTION]`, `> [!WARNING]`
- Do not make unnecessary changes to existing code

## Project-Specific Patterns
### Configuration Access
```csharp
// Use dot notation for nested config access
var isEnabled = _appConfigs.Get<bool>(path: "Security.Cors.Enabled");
var maxItems = _appConfigs.Get(path: "app.settings.maxItems", defaultValue: 100);
```

### Middleware Configuration
- Middleware order matters: Request Logging → Rate Limiting → Response Compression → Security Headers → API Key Auth
- All middleware is conditionally enabled via `appconfigs.jsonc`
- API Key Authentication excludes paths: Swagger UI, CSS, JS, themes, images

### Controller Organization
- Split controllers by feature area into separate partial files
- Use `[ApiExplorerSettings(GroupName = "Demo")]` for Swagger grouping
- Primary constructor injection pattern throughout

### Dependency Injection Setup
- All DI configuration in `Program.DI.cs` via `SetupDI()` method
- Language, logging, caching, databases configured separately
- Scoped registration for middleware: `builder.Services.AddScoped<ApiKeyAuthenticationMiddleware>()`

## Development Workflow
- Build scripts for cross-platform: `installer.ps1`, `installer.sh`
- Swagger UI with theme switching at runtime
- Dispose pattern implementation required for configuration handlers

## Method Variants
- Create async versions with callback parameters and cancellation tokens
- Do not modify existing synchronous methods when adding async variants

## Communication
- Explanations in Bahasa Indonesia when needed
- Code speaks for itself - minimal explanations required