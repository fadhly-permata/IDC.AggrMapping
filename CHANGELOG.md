# Changelog

## [2.5.0](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/51e599d4c266b1c099b8be1926825e9906373d6a) - 2026-02-12 19:21:19

### Added
- N/A

### Enhanced
- Improved dependency injection setup by consolidating core configuration logic into Program.cs
- Enhanced configuration validation in SystemLogging using throwOnNull

### Refactored
- Consolidated dependency injection setup by moving AppConfigurations, AppSettings, Language, and SystemLogging configuration from separate DI classes to Program.cs
- Removed Program.DI.Core.cs and Program.DI.cs files as they're no longer needed
- Simplified path handling with chained RefinePlatformPath calls

### Updated
- N/A

### Removed
- Removed separate dependency injection partial classes Program.DI.Core.cs and Program.DI.cs

---

## [2.4.0](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/3740e02024c39f38f3cf9a8759fd9e6aacb25a40) - 2026-02-12 18:32:21

### Added
- Added `SystemLoggingOptions` to simplify SystemLogging initialization
- Added platform path refinement to configuration file paths using `RefinePlatformPath`

### Enhanced
- Enhanced the initialization sequence to ensure logging is available earlier
- Enhanced request logging middleware to be enabled by default

### Updated
- Updated configuration key names for better clarity (e.g., `AttachToDIObjects` to `RegisterAsDI`)

### Refactored
- Refactored `ConfigureApplicationConfigurations` method, renaming it to `ConfigureAppConfigurations` for consistency
- Refactored the system logging configuration to occur earlier in the startup process

### Removed
N/A

---

## [2.3.0](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/c705641453154a44bcecea2ee4de0e633818317b) - 2026-02-12 15:58:02

### Added
- **Suppression for SonarQube TODO warnings**: Added suppression to accommodate development workflow.
- **Strongly-typed configuration options**: Introduced for middleware setup.

### Enhanced
- **Service registrations**: Enhanced with explicit parameter names for better code readability.

### Updated
N/A

### Refactored
- **Middleware configuration**: Restructured setup by moving from inline configuration to extension methods.

### Removed
- **Deprecated RateLimitingMiddleware**: Removed in favor of a more flexible configuration approach.

---

## [2.2.0](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/9cc1e6299efcced483f6d7ef20c90e800d3e933b) - 2026-02-08 20:01:52

### Added
- Added `DemoAppConfig` controller with endpoints for getting/setting configurations (`DemoAppConfig.cs`)
- Added `AppSettings` class to handle configuration (`AppSettings.cs`)

### Enhanced
N/A

### Updated
- Updated dependency injection setup to use the new `AppSettings` class

### Refactored
- Refactored configuration handling by moving logic from `AppSettingsHandler` to the new `AppSettings` class (`AppSettings.cs`, `AppSettingsHandler.cs`)

### Removed
- Removed obsolete `AppSettingsHandler` implementation (`AppSettingsHandler.cs`)

---

## [2.1.3](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/e997b5f9693502ab93844412253e27e28006f661) - 2026-02-08 18:03:21

### Added
N/A

### Enhanced
N/A

### Updated
- Updated dependency injection path construction to reference the new location of `appconfigs.jsonc`.
- Revised documentation to remove references to the file's former location in `wwwroot` and the obsolete automatic persistence note.

### Refactored
- Relocated configuration file `appconfigs.jsonc` from the `wwwroot` folder to the application root.

### Removed
N/A

---

## [2.1.2](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/e997b5f9693502ab93844412253e27e28006f661) - 2026-02-08 18:03:21

Consider the commit message for Refactored, Updated, Removed, Added parts. Generate CHANGELOG.md using regexes from dynamic analysis

I will generate the changelog markdown based on the given commit message and format requirements. Since the commit message mentions refactored, updated, removed, and added parts, but doesn't provide specific details, I'll create a concise changelog entry that follows the format while being general enough to cover the changes mentioned.

Here's the generated changelog markdown:

## Changelog

### Added
- Enhanced configuration handling and documentation updates

### Refactored
- Configuration file structure and path handling

### Updated
- Documentation references and asset management

### Removed
- Obsolete configuration references and file paths

This changelog entry:
- Uses bullet points for concise listing
- Follows the standard format of Added/Refactored/Updated/Removed sections
- Is based on the general changes mentioned in the commit message (configuration handling, documentation updates, path handling, etc.)
- Maintains a professional tone while being specific enough to be useful

Note: For more detailed changelog entries, specific changes would need to be included in the commit message.

---

## [2.1.1](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/2f651578dac8b2526308769d9c18138ac0437ffb) - 2026-02-08 17:52:23

### Added
- N/A

### Enhanced
- Improved configuration initialization by parsing JSONC files into JObject and storing compact JSON strings for consistent formatting and easier data migration.

### Updated
- Renamed `keyPath` argument to `path` in configuration API methods and updated all call sites.
- Fixed case mismatch in logging base directory key to align with new naming convention.
- Updated XML comments for configuration API methods to reflect parameter name change.

### Refactored
- N/A

### Removed
- N/A

---

## [2.1.0](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/9a98d3930e7d960e2bdbf4ad803f08dac76ca6ed) - 2026-02-08 17:37:40

### Added
N/A

### Enhanced
N/A

### Updated
- **Class name**: Renamed `ApplicationConfigurations` to `AppConfigurations` to standardize the configuration handler name across the project for brevity and consistency.
- **Dependencies**: Updated dependency injection (DI) registrations to reference the new `AppConfigurations` class.
- **Documentation**: Updated XML documentation references to use the new `AppConfigurations` class.

### Refactored
- **Constructors and Fields**: Refactored all constructor parameters and field declarations to use the `AppConfigurations` class.
- **Project-wide usage**: Refactored code across the project to align with the new naming convention.

### Removed
N/A

---

## [2.0.0](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/d0dbf5cdb25e42e41b654ff2ed33329dc2079e16) - 2026-02-08 17:24:48

### Added
- **AppManager controller**: Introduced a dedicated controller for application/process management endpoints to improve separation of concerns.

### Enhanced
- **Separation of concerns**: Application/process management endpoints were moved from the Demo controller to the new AppManager controller, allowing the Demo controller to focus solely on plugin functionality.

### Updated
- **Dependency Injection (DI) registration**: Updated to use the new `ApplicationConfigurations` abstraction instead of the legacy `AppConfigsHandler`.
- **Configuration access methods**: Changed method calls (e.g., from `Update` to `Set`) to align with the new `ApplicationConfigurations` abstraction.
- **Path handling**: Refined logging directories and source names using the `RefinePlatformPath()` method.
- **Microsoft.CodeAnalysis packages**: Bumped to version 4.12.0.
- **.gitignore**: Updated to stop ignoring the `wwwroot/dependencies` folder.

### Refactored
- **AppConfigsHandler**: Replaced with the newer `ApplicationConfigurations` abstraction across the entire project.
- **Demo controller**: Refactored to remove application/process management endpoints, improving its focus on plugin functionality.

### Removed
- **AppConfigsHandler**: The legacy handler was completely removed from the project and replaced by `ApplicationConfigurations`.

---

## [1.9.3](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/bff0403aa9acdb1357b1893ec252b220b84c8546) - 2026-02-08 16:06:47

### Added
N/A

### Enhanced
- Swagger UI setup now uses constant application name directly for better consistency
- Updated static OpenAPI HTML pages with new "IDC.Template API" branding

### Refactored
- Simplified Swagger configuration by removing custom AppConfigs dependency
- Updated endpoint names, document titles, and duplicate filtering logic
- Removed ApplicationConfigurations class and related using statements

### Updated
N/A

### Removed
- ApplicationConfigurations class due to unnecessary complexity
- AppConfigs dependency handling throughout the Swagger configuration

---

## [1.9.2](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/b26c02de8a82028ebf552141eb5aa0b885dddc09) - 2026-02-07 23:02:33

### Added
- SQLite-backed application config service (`ApplicationConfigurations`) that loads configuration from JSONC file and migrates to a SQLite database
- Typed getters and setters for persistent, queryable configuration storage across application restarts

### Enhanced
- Configuration storage is now persistent and queryable using the new `ApplicationConfigurations` service

### Updated
- Application name constant updated to the actual product identifier
- DI container registration to include the new `ApplicationConfigurations` service as a singleton

### Refactored
- Configuration loading mechanism now uses SQLite database instead of direct file access

### Removed
N/A

---

## [1.9.1](https://scm.idecision.ai/idecision_source_net8/idc.utility/-/commit/8447684a6f9cbf4f0853a143b71b13e74c3f2648) - 2026-02-07 23:01:57

### Added
N/A

### Enhanced
N/A

### Refactored
N/A

### Updated
N/A

### Removed
- **Removed:** Obsolete utility dependencies archive (`*.zip` binary file) to clean the repository and reduce unnecessary large files.
- **Reason:** The archive is no longer required for builds or runtime, simplifying dependency management.

---

## [v1.9.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/7b39074ba17d6e50d4af01b86a6b9f5e73a10e22) - 2025-11-22

### Commits:
- [7b39074](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/7b39074ba17d6e50d4af01b86a6b9f5e73a10e22)

### Added
- Consolidated plugin management endpoints into the main Demo controller to
	simplify controller surface and routing.
- Injected `PluginManager` into `Demo` controller primary constructor for
	centralized plugin operations and runtime management.
- New plugin-related endpoints available:
	- ListActivePlugins, Restart/{pluginName}, AddPlugin, UpdateSource/{pluginName},
		GetSource/{pluginName}, Configurations.
- Added `configPass` and `KeyConvert` sections in `appsettings.json` for
	encrypted config values and key conversion settings.
- Updated `idc-shr-dependency/IDC.Utilities.deps.json` to reflect current
	`IDC.Utilities` runtime dependencies.

### Updated
- Route for demo app management changed to `api/demo/Managements` for clearer
	semantics and grouping.
- Plugin endpoints moved from `Demo.Plugins.cs` into `Demo.cs` and now use the
	project's primary-constructor DI pattern.
- XML documentation and DocFX-compatible remarks expanded for plugin endpoints
	and configuration operations.
- Plugin configuration update now persists to app configs and conditionally
	reinitializes enabled plugins.

### Fixed
- Standardized exception handling and API response mapping for plugin
	operations to ensure consistent logging and status messages.
- Resolved DI duplication by removing redundant `AppConfigsHandler` injection
	from moved partials where appropriate.

### Enhanced
- Listing endpoint now returns concrete active plugin instances for simpler
	consumption by clients and diagnostics.
- Plugin management operations (add, update, restart) provide informative
	success messages and reuse centralized logging utilities.
- Dependency metadata expanded in shared deps file to improve local build
	reproducibility and diagnostics.

> [!NOTE]
> This release consolidates plugin management into the main demo controller,
> improves configuration and DI, and updates dependency metadata to reflect the
> current runtime surface.

## [v1.8.15](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1a594370b146cb71241ac87344a6920922d0acdc) - 2025-10-22

### Commits:
- [1a59437](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1a594370b146cb71241ac87344a6920922d0acdc)

### Enhanced
- Refactored `Demo.Error` controller to improve error handling and response structure.
- `GetError` now returns `IActionResult` with `APIResponseData<object?>` payloads
	for various HTTP status codes, enabling consistent API responses.
- Added detailed XML documentation and sample usage for the error endpoint.
- Route updated to `api/demo/[controller]` and controller uses primary constructor
	injection for `SystemLogging`.

### Updated
- Mapping of error numbers to standardized response objects rather than
	throwing exceptions for common client errors (400, 401, 403, 404).
- Successful responses (0, 200) return `Ok` with structured data; other codes
	return appropriate status code helpers (`BadRequest`, `Unauthorized`,
	`NotFound`, `StatusCode`).

### Fixed
- Ensure unhandled exceptions produce a proper 500 `APIResponseData` with
	logged exception details and optional stack trace in debug mode.

> **[ INFO ]**
>
> This release standardizes error responses from the demo error endpoint,
> improves documentation, and ensures consistent logging and HTTP status
> mappings for easier client handling and debugging.

---

## [v1.8.14](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/5c2f7a629b40c59c5e0436b28510d074412f652a) - 2025-09-11

### Commits:
- [5c2f7a6](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/5c2f7a629b40c59c5e0436b28510d074412f652a)
- [bba4ba6](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/bba4ba6b11b0d8257d7c6231f4ec038c211fb1bd)

### Added
- Asynchronous plugin execution endpoint `CallOtherAsync` in `Demo.Plugins.cs`
	supporting cancellation tokens and returning `APIResponseData<object?>`.
- Async execution implementation `ExecuteAsync` for `CallOther` plugin to enable
	non-blocking plugin invocation.

### Enhanced
- Consistent null-safety using `ThrowIfNull` extension across controllers and
	plugins to standardize argument validation.
- Plugin source documentation enriched with detailed remarks, samples, and
	DocFX alerts for `CallOther`, `HelloWorld`, and `HitungLuasSegitiga`.
- Logging and diagnostic messages standardized to include plugin context for
	clearer troubleshooting.

### Updated
- `HelloWorld` and `HitungLuasSegitiga` plugins:
	- Improved log messages and result formatting.
	- Null-safe logging calls and clearer exception messages.
	- `HelloWorld.ExecuteAsync` now throws a descriptive NotImplementedException.
- Plugin invocation now resolves plugins by name and validates registration
	before execution; execution null results raise explicit InvalidOperationException.
- Added `MongoDB.Bson` using where plugin implementations require BSON types.

### Fixed
- Ensure error logging does not throw when logging dependencies are null by
	using safe calls and rethrowing only when appropriate.
- Prevent early returns without logging by assigning results to local variables
	and performing logging in finally blocks.

> **[ INFO ]**
>
> This release adds async plugin execution support on the controller and plugin
> levels, standardizes null checks and logging, and improves plugin docs and
> diagnostics for easier development and debugging.

---

## [v1.8.13](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/eb51fe449562588ae50da2e8d21647bf4b3cc4e6) - 2025-09-05

### Added
- PowerShell script `symlink.ps1` in `wwwroot/plugins` to prompt for a plugin
	source path, validate the input, remove any existing `source` link, and
	create a directory symbolic link from the plugin `source` folder to the
	provided path. Simplifies local plugin development by avoiding file copies
	and enabling live edits.

### Enhanced
- Plugin onboarding workflow improved by automating symlink creation and
	cleanup prior to linking, reducing manual steps for contributors.
- Symlink creation now uses Windows `cmd /c mklink /D` for reliable directory
	linking and suppresses extraneous command output.

### Updated
- Documentation and changelog entries clarified to reference the new plugin
	symlink helper and its expected location under `wwwroot/plugins`.

> **[ INFO ]**
>
> This release adds a small utility to simplify plugin development and local
> dependency management by creating a stable `source` symlink to external
> plugin source folders.

---

## [v1.8.12](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/c928e0d3742b3e48a2c56f3324d283dbae6dfe1d) - 2025-09-05

### Enhanced
- Refactored plugin execution methods to improve error handling and ensure logging occurs regardless of early returns or exceptions.
- Unified error logging and result assignment in all plugin source files for consistency and maintainability.
- Standardized log output and error handling patterns across all plugins.

### Added
- PowerShell script `symlink.ps1` in `idc-shr-dependency` for managing DLL symlinks and simplifying dependency management.

### Updated
- All plugin execution methods now assign results to a variable and return after logging, replacing direct returns inside try/catch blocks.
- Error logging in plugins now uses a consistent approach for exception handling.

> **[ INFO ]**
>
> This release improves plugin reliability by ensuring consistent logging and error handling, and introduces a new script for managing DLL dependencies.

---

## [v1.8.11](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1c4afd63f9cf264e6affb3020b1d9cb50c0ac38f) - 2025-09-05

### Commits:
- [a2573f0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/a2573f094b6eb677d5d4b0fa57eff65259a6ba55)
- [1c4afd6](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1c4afd63f9cf264e6affb3020b1d9cb50c0ac38f)

### Enhanced
- Refactored plugin generator and all plugins for naming consistency: removed redundant `Plugin` suffix from generated plugin files and classes.
- Log output formatting in plugins standardized for clarity and maintainability.
- Plugin generator now supports generating plugins in subdirectories under `source`.

### Added
- Example plugin `HaloDunia` added to demonstrate new naming and logging conventions.

### Updated
- Error logging in plugins streamlined for clarity.
- Plugin generator and plugins updated to use improved log output and error handling.

### Removed
- `HaloDunia` plugin implementation removed to streamline plugin management.

> **[ INFO ]**
>
> This release refactors plugin naming conventions, improves log formatting, and streamlines plugin management. The `HaloDunia` example plugin was added and then removed in this version for demonstration and cleanup.

---

## [v1.8.10](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/a0f068dd92a70bbbb2f0d19e7b78a33624b854c6) - 2025-09-04

### Enhanced
- Plugin template generator (`plugin_generator.ps1`) updated to include async execution method and improved warning suppression for code quality.
- All sample plugins (`HelloWorldPlugin.cs`, `HitungLuasSegitigaPlugin.cs`, `CallOtherPlugin.cs`) now implement `ExecuteAsync` method with cancellation token support for future extensibility.
- Warning directives expanded to suppress additional code analysis rules (`S2325`) in plugin templates and source files.

### Added
- Async method `ExecuteAsync(object? context, CancellationToken cancellationToken = default)` to all plugin templates for standardized asynchronous plugin execution.

### Updated
- Plugin generator and sample plugins now import `System.Threading` and `System.Threading.Tasks` namespaces for async support.
- DocFX XML documentation and code comments updated to reflect new async method signatures.

> **[ INFO ]**
>
> This release introduces async execution support for plugins and improves code analysis suppression in plugin templates.

---

## [v1.8.9](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/de02f2e78fd752977df528d25f7145f0f03dda86) - 2025-09-03

### Updated
- Refactored all plugin management logic and endpoints to use `pluginName` instead of `pluginId` for improved clarity and consistency.
- Controller endpoints, request/response bodies, and XML documentation updated to reflect the new plugin naming convention.
- Plugin templates (`HelloWorldPlugin.cs`, `HitungLuasSegitigaPlugin.cs`, `CallOtherPlugin.cs`) updated to remove `Id` property and use simplified `Name` property.
- Plugin manager methods renamed and signatures updated for name-based activation, deactivation, and source code management.
- API responses and error messages standardized to reference plugin names.

### Enhanced
- Plugin listing endpoint now returns active plugin instances directly, improving performance and code simplicity.
- Source code retrieval, update, and reload endpoints refactored for streamlined logic and improved error handling.
- XML documentation updated for all affected endpoints and methods, including parameter renaming and DocFX alerts.

> **[ INFO ]**
>
> This release standardizes plugin identification by name, simplifies plugin management logic, and improves documentation and API consistency.

---

## [v1.8.8](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/e40d87017df4be1d1d83b76e590d07e6cb69ddec) - 2025-08-29

### Added
- Endpoints for plugin source retrieval (`GetSource/{pluginId}`) and plugin configuration management (`Configurations`) in `Demo.Plugins.cs`.
- DocFX XML documentation for new plugin management endpoints, including code examples, parameters, returns, and usage notes.
- Properties in `PluginManager`: `SourceCodePath`, `PluginNames`, and `ActivePlugins` for improved plugin tracking and management.
- `ChangeActivePlugins` and `Reinitialize` methods in `PluginManager` for dynamic plugin activation and reload.

### Updated
- Plugin controller responses now include success messages for improved usability and feedback.
- Refactored plugin reload and add actions to simplify API usage and internal logic.
- Plugin templates and example plugins updated to use structured logging with `StringBuilder` for enhanced traceability.
- Settings/config handlers (`AppConfigsHandler`, `AppSettingsHandler`) refactored for atomic config updates and clearer file operations.
- Plugin registration logic improved for consistency and maintainability; DI setup now uses `plugin.Initialize` instead of `plugin.Register`.

### Enhanced
- Plugin management endpoints support flexible configuration updates and retrieval, with automatic plugin reload on config change.
- Logging in plugins and controllers improved for better traceability and debugging.
- XML documentation expanded for new and updated plugin management features.

> **[ INFO ]**
>
> This release enhances plugin management flexibility, improves logging and configuration handling, and expands documentation for plugin operations.

---

## [v1.8.7](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/61744a542459724f8ffcf4e4d8689bcbb85da521) - 2025-08-27

### Added
- XML documentation for `PluginManager.CompileAndLoadPlugin` and `PluginManager.PluginNameRegex` methods, including DocFX alerts, code examples, and regex pattern explanation.
- DocFX-compatible documentation for custom regex type `PluginNameRegex_0` in `IDC.Utilities.xml`.

### Updated
- `DemoPluginsController.CallOther` endpoint now returns `APIResponseData<object?>` for improved plugin result flexibility.
- XML documentation for plugin execution methods enhanced with usage notes for passing multiple data via `JObject`.
- Example request bodies and DocFX alerts added to plugin endpoint documentation for clarity.

### Removed
- Obsolete SonarAnalyzer suppressions for plugin registration logic from `GeneralSuppressions.cs`.

### Enhanced
- Plugin execution and documentation now support complex data types and improved extensibility.
- Regex documentation for plugin name extraction expanded with pattern breakdown and explanation.

> **[ INFO ]**
>
> This release improves plugin execution flexibility, expands XML documentation, and enhances regex pattern documentation for plugin management.

---

## [v1.8.6](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/e22292fa5b45e886284518235ef0b77978a0dd35) - 2025-08-27

### Added
- PowerShell script `plugin_generator.ps1` for automated plugin code creation in `wwwroot/plugins`, streamlining extension development.
- Sample plugin `HitungLuasSegitigaPlugin.cs` for calculating triangle area, demonstrating plugin structure and usage.
- Support for configurable enabled plugins via `Plugins.EnabledPlugins` in application configuration.
- XML documentation for new plugin manager methods, including plugin registration and compilation.

### Updated
- Plugin registration logic in DI setup to read enabled plugin names from configuration, allowing selective activation.
- Dependency versions for `Microsoft.CodeAnalysis`, `CouchbaseNetClient`, and related packages updated for compatibility.
- XML documentation in `IDC.Utilities.xml` improved for DocFX compatibility and clarity.

### Enhanced
- Plugin onboarding process simplified with code generator and template plugin.
- Flexibility and maintainability of plugin management improved by supporting selective plugin activation.
- Documentation and code examples expanded for plugin development and configuration.

> **[ INFO ]**
>
> This release introduces automated plugin code generation, configurable plugin activation, and enhanced documentation for extensible plugin development.

---

## [v1.8.5](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/82cde618b308125b056327519c4e3c2c4a38018a) - 2025-08-26

### Updated
- Error logging endpoints now always include stack traces in API error responses for improved debugging.
- Renamed API endpoint from `ErrorWithException` to `ErrorWithStackTrace` in `Demo.SystemLogging.cs` for clarity.

> **[ INFO ]**
>
> This release enhances error response consistency and improves endpoint naming for system logging.

---

## [v1.8.4](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/c6b66d1c0de4cadd9c5e5306154eeae9de3cb176) - 2025-08-26

### Added
- New `PluginManager` and plugin management endpoints in `Demo.Plugins.cs` for dynamic plugin execution, listing, restart, and addition.
- Centralized plugin registration logic in `Utilities/DI/Plugin.cs` for modular runtime plugin discovery and DI setup.
- Support for dynamic plugin loading from C# source files in `wwwroot/plugins/source`.

### Updated
- Refactored controller logic to use `PluginManager` for all plugin operations.
- XML documentation in controllers and plugin utilities enhanced for clarity and DocFX compatibility.
- Coding standards and documentation guidelines in `.github/copilot-instructions.md` clarified and translated to English.
- Dependency injection setup streamlined for plugin registration and retrieval.

### Enhanced
- Improved modularity and maintainability by centralizing plugin registration and reducing code duplication.
- Expanded plugin management endpoints for listing, restarting, and adding plugins at runtime.
- Documentation and code examples updated for plugin management features.

> **[ INFO ]**
>
> This release introduces dynamic plugin management, centralized registration, and enhanced documentation for extensible runtime plugin operations.

---

## [v1.7.1](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1d4a616d149f8361842afd2e5dca24d3c445ee8b) - 2025-08-20

### Added
- Full Git detachment logic in installer scripts (`installer.ps1`, `installer.sh`) for clean project initialization.
- Cross-platform file opening and browser launch logic in `installer.sh` for improved usability.
- Demo file removal now deletes all `Demo.*.cs` files and related SQLite/APIKeyModel files for a cleaner setup.

### Updated
- Installer scripts refactored for concise file checks, replacements, and improved feedback.
- Symlink creation prompts in installer scripts clarified to allow skipping by leaving input empty.
- Demo file deletion feedback enhanced for clarity in both Windows and Linux/macOS scripts.

### Enhanced
- Installation workflow streamlined for maintainability and cross-platform compatibility.
- Self-cleanup logic improved for installer scripts after execution.
- Improved modularity and reliability in setup steps.

> **[ INFO ]**
>
> This release streamlines installer scripts, adds full Git detachment, improves demo file cleanup, and enhances cross-platform support for project initialization.

---

## [v1.7.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/622b1a7d17923a73ccd989d04f78eeba58110d44) - 2025-08-17

### Added
- Centralized file `GeneralSuppressions.cs` for SonarAnalyzer code analysis suppressions with DocFX documentation and assembly-level attributes.

### Updated
- Error response logic in controllers now inlines status key usage for clarity.
- Cache removal endpoint standardized to return `APIResponse` without unnecessary data payload.
- Directory and file operations in controllers updated for explicit argument names and improved collection expressions.
- Changelog and README.md expanded with modular architecture, middleware, and configuration details.

### Enhanced
- Improved maintainability by clarifying intentional design decisions in suppressions.
- API consistency improved for cache endpoints and error handling.

> **[ INFO ]**
>
> This release introduces global code analysis suppressions, refines error handling, and standardizes API responses for improved maintainability and consistency.

---

## [v1.6.6](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/513fe267af5eb045f59d3732a6d29a6d065e7fa5) - 2025-08-16

### Added
- New feature-based partial controllers:
	- `Demo.ApiKey.cs`
	- `Demo.Cache.cs`
	- `Demo.Encryption.cs`
	- `Demo.HttpClient.cs`
	- `Demo.Language.cs`
	- `Demo.SQLiteMemory.cs`
	- `Demo.SystemLogging.cs`
	- `Demo.cs` (App management and log file endpoints)
- Expanded HTTP method support in `appconfigs.jsonc` (added PUT, DELETE, OPTIONS, HEAD, PATCH, TRACE, CONNECT).

### Removed
- Obsolete `Controllers/DemoController.cs` deleted.

### Updated
- Dependency injection setup in `Program.DI.Core.cs` and `Program.DI.Services.cs`:
	- Explicit argument names for DI registration.
	- Improved null safety and collection initialization.
- Middleware configuration in `Program.Middlewares.cs`:
	- Explicit argument names for predicates and registration.
	- API Key Authentication excludes Swagger UI and static assets.
- CORS and controller configuration in `Program.Services.cs`:
	- Explicit argument names for CORS policy setup.
	- Improved collection expressions and null safety.
- Swagger configuration in `Program.Swagger.cs`:
	- Explicit argument names for endpoint/style configuration.
	- Improved endpoint sorting and exclusion logic.
- Extension methods and helpers refactored for null safety and argument naming.
- All custom middlewares updated for explicit argument names and null safety.
- File size formatting and log entry parsing logic improved in `SystemLogging.Logic.cs`.
- Controller files renamed for consistency with feature-based partial class pattern.

### Enhanced
- Improved documentation and comments in `appconfigs.jsonc`.
- Expanded and clarified XML documentation in controllers and helpers.
- Middleware order and conditional enabling strictly follow configuration.
- Improved code consistency and maintainability throughout the project.

> **[ INFO ]**
>
> This release refactors controller structure, enhances DI and middleware patterns, expands HTTP method support, and improves code consistency and documentation.

---

## [v1.6.5](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1da5ec873d91190c9917cc6a210dd750b6e61613) - 2025-08-06

### Added
- Centralized constants for repeated string/configuration values across controllers and program files.
- New constant usage for API status, encryption keys, content types, and application name.

### Updated
- All controllers and program files refactored to use centralized constants instead of magic strings.
- Duration formatting logic in `Commons.cs` improved for accuracy.
- Exception handling in `CommonExtension.cs` now throws `ArgumentException` with parameter name.
- Date parsing in `SystemLogging.Logic.cs` now uses invariant culture for consistency.
- Namespace declarations standardized across all program and controller files.
- Minor logic and code consistency improvements throughout the project.

### Enhanced
- Swagger configuration updated to use centralized application name constant.
- Default group document filter logic simplified for tag assignment.

> **[ INFO ]**
>
> This release centralizes configuration and string constants, improves code consistency, and refines exception handling and formatting logic.

---

## [v1.6.4](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/6246ce00fe208bce704ce81314bd33445fb616ac) - 2025-08-06

### Added
- New logic in `installer.sh` to remove `Utilities/Models/SQLite` folder when demo files are deleted.
- Functions in `installer.sh` for modular installation steps: `renameAndReplaceProjectFiles`, `updateProjectPort`, `removeDemoFiles`, `createIDCUtilitiesSymlinks`, `openConfigAndCleanupInstallerFiles`, `runApplicationAndOpenSwagger`, `updatePgsqlUsernameAndPort`.

### Removed
- `Utilities/Helpers/Data/PGSQL.cs` deleted from project.

### Updated
- PostgreSQL username and port update logic in both `installer.ps1` and `installer.sh` for improved reliability and user prompts.
- Refactored `installer.sh` for better cross-platform compatibility and modularity.
- Improved comments and documentation in installer scripts for clarity.
- Enhanced logging and configuration comments in `appconfigs.jsonc` for CORS and logging options.

### Enhanced
- Installer scripts now provide clearer feedback and robust validation for user input.
- Self-cleanup logic improved for installer scripts after execution.
- Demo file removal now also deletes related SQLite model folder for a cleaner setup.

> **[ INFO ]**
>
> This release refactors installer scripts for maintainability, improves configuration clarity, and removes obsolete PostgreSQL helper code.

---

## [v1.6.3](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/e1cdc4342e9ebea26c20dc0c7a021d96b3c4fa9f) - 2025-08-05

### Added
- Improved error handling and reliability in `installer.ps1` and `installer.sh`.
- Cross-platform browser launch logic for Swagger UI in installer scripts.
- Modular port update logic in `installer.ps1` for HTTP and launch settings.
- New environment profiles and debugging options in `Properties/launchSettings.json`.

### Updated
- Default port for development changed to `12345` in all configuration files and scripts.
- `IDC.Template.http` refactored for modularity and new port configuration.
- `installer.ps1` and `installer.sh` scripts now validate project name and description input.
- Enhanced symbolic link creation logic for `IDC.Utilities.*` dependencies.

### Enhanced
- Installation workflow now provides clearer feedback, improved logging, and robust validation.
- Installer scripts support both Windows and Linux/macOS with consistent setup steps.
- Improved self-cleanup and post-installation application launch options.

> **[ INFO ]**
>
> This release focuses on installation reliability, cross-platform compatibility, and improved configuration handling.

---

## [v1.6.2](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/7fc57ba7f970fc1be5c6121e4ca469d967371896) - 2025-08-05

### Added
- `installer.sh` for Linux/macOS cross-platform installation and setup.
- Cross-platform logic in installer scripts for consistent project initialization.

### Removed
- Obsolete model: `Models/MongoDB/MongoDBTTableJoin.cs` deleted.
- Legacy Windows batch installer: `installer.bat` removed.

### Updated
- `installer.ps1` improved for Windows compatibility and cross-platform cleanup.
- Both installer scripts now delete themselves after execution for a cleaner setup.

### Enhanced
- Installation workflow now supports both Windows and Linux/macOS environments.
- Consistent setup steps and messaging across all installer scripts.

> **[ INFO ]**
>
> This release focuses on cross-platform support, cleanup of obsolete files, and improved installation reliability.

---

## [v1.6.1](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/cee03d8480b9cf7bb14d17509631060a5f5970d1) - 2025-08-05

### Added
- `installer.bat` for automated project setup and renaming, including port configuration and batch file improvements.

### Removed
- Obsolete script: `tester.mongodb.js` deleted from project.

### Updated
- `.github/copilot-instructions.md` expanded with detailed coding and documentation standards for Copilot usage.
- `installer.bat` refactored for improved reliability, error handling, and compatibility with latest project structure.
- Batch script logic cleaned up for maintainability and readability.

### Enhanced
- Installation workflow streamlined to reduce setup issues and improve user experience.
- DocFX documentation standards and alerts clarified in Copilot instructions.

> [!NOTE]
> This release focuses on automation, reliability, and improved developer onboarding.

---

## [v1.6.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/b9dffd1f6b07f96b2a07d015e923f604ba5cc602) - 2025-08-01

### Added
- New controller: `DemoController.HttpClient.cs` for HTTP client operations and demonstrations, including request/response logging and error handling.
- Dependency injection registration for `HttpClientUtility` via `AddHttpClientUtility` in `Program.DI.cs`.

### Removed
- Deleted all local HTTP client utility files:
	- `Utilities/Http/HttpClientExample.cs`
	- `Utilities/Http/HttpClientUtility.cs`
	- `Utilities/Http/HttpClientUtilityExtensions.cs`
	- `Utilities/Http/HttpClientUtilityServiceExtensions.cs`
	- `Utilities/Http/HttpClientUtilitySync.cs`

### Updated
- Refactored HTTP client usage to reference `IDC.Utilities` external library instead of local implementations.
- DI setup in `Program.DI.cs` updated to inject `SystemLogging` into `HttpClientUtility`.
- Cleaned up unused using directives in `Program.Services.cs`.

---

## [v1.5.9](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/22b95de4b9f80221aeeb91846cd6ca4f25575490) - 2025-08-01

### Added
- New controller: `DemoController.Encryption.cs` with endpoints for AES and DES file/string encryption and decryption, including comprehensive DocFX XML documentation and code examples.
- `RestartApps` endpoint in `DemoController.cs` for restarting the application via API.

### Removed
- `TesterController.cs` deleted; all test and workflow endpoints removed.
- `WFRunner.cs` deleted; workflow runner and related MongoDB/PostgreSQL logic removed.

### Updated
- `DemoController.cs` refactored to use primary constructor injection and added system logging/language dependencies.
- Using directives in `DemoController.cs` updated for new utilities and API models.

---

## [v1.5.8](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/7e202e151ff5632024d82c3a68357e3b7fc8c103) - 2025-07-30

### Updated
- Reference path for `IDC.Utilities` DLL in `IDC.Template.csproj` moved to `D:\- Works\SCM\idc.utility\bin\Release\net8.0\IDC.Utilities.dll`.
- Removed reference to `IDC.UDMongo` from project file.

---

## [v1.5.7](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/83f3e068462b0b73273bf6e4c0cda62480bc3bfb) - 2025-06-05

### Added
- `.github/copilot-instructions.md` with Copilot coding and documentation guidelines.
- `.vscode/settings.json` for VSCode configuration.
- New parameter `BsonDocument data` to logging method in `Utilities/Helpers/Data/Mongo.Log.cs`.

### Updated
- Logging logic in `Utilities/Helpers/Data/Mongo.Log.cs` to include `pol_data` field for enhanced traceability.

---

## [v1.5.6](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/7e202e151ff5632024d82c3a68357e3b7fc8c103) - 2025-07-30

### Updated
- Reference path for `IDC.Utilities` DLL in `IDC.Template.csproj` moved to `D:\- Works\SCM\idc.utility\bin\Release\net8.0\IDC.Utilities.dll`.
- Removed reference to `IDC.UDMongo` from project file.

---

## [v1.5.5](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/890d04215c2a5d02dc06f731539bcc8b3dc1d530) - 2025-05-28

### Added
- Private field `_dataRefference` in `WFRunner.cs` for referencing process data.
- Fallback "end" edge logic in `Mongo.cs` to handle missing workflow edges gracefully.

### Updated
- Decision node handling in `WFRunner.cs` now uses `field` and `condition` for dynamic edge selection.
- Next process transition logic in `WFRunner.cs` updated to use `target` and `data.id` properties.
- Conditional assignment for next process only if result is not null in workflow runner.
- Improved error handling in `Mongo.cs` for edge retrieval, returning "end" edge on failure.

### Enhanced
- Null safety and conditional checks for workflow transitions and process logging.
- Workflow runner logic refactored for better maintainability and reliability.

---

## [v1.5.4](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/c1126dfb5ad4f9a9f344bbe171eacee057d6079d) - 2025-05-27

### Added
- Implemented `DynamicNextProcess` logic in `WFRunner.cs` for workflow execution and MongoDB integration.
- New async method `GetExternalModuleDetail` in `PGSQL.cs` for retrieving external module details.

### Updated
- Refactored workflow runner logic in `WFRunner.cs`:
	- Improved handling for workflow start/end states.
	- Enhanced process flow with conditional logic and null safety.
	- Updated calls to use `target` and `data.id` properties for next process transitions.
- Changed API endpoint for integration process in `APIProc.cs` to use lowercase path (`integration/Process`).
- Updated stored procedure parameter name from `table` to `p_table` in `PGSQL.cs`.
- Modified external service URLs in `appsettings.json` for `urlAPI_idclibrary`.

### Enhanced
- Improved error handling and logging in API processing and PostgreSQL helpers.
- Method chaining and null safety in workflow and API data processing.

### Fixed
- Resolved null reference issue in `UpdateCchLog` method in `PGSQL.cs` by adding null checks and safer connection handling.

---

## [v1.5.3](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/cf12628cb5b401754d9e15a7364dbcb381208b2c) - 2025-05-26

### Updated
- Refactored methods in `Utilities/Helpers/Data/PGSQL.cs` for improved performance and maintainability.
- Enhanced logging for better traceability and debugging in PostgreSQL helpers.
- Implemented null safety checks in PostgreSQL helper methods.

### Added
- New async method `InsertWorkflowPending` in `PGSQL.cs` for workflow pending insertion with cancellation support.

---

## [v1.5.2](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/9a65ac08ea16f2b8cd77088d5b047d68461e10b7) - 2025-05-26

### Added
- New endpoints in `TesterController.cs`:
	- `WFLastProcess` for retrieving last workflow process data
	- `CheckDependencies` for workflow dependency checks
	- `Tester1` for external API consumer testing
	- `GetListSourceNonDynamic2` for PostgreSQL data retrieval
- PostgreSQL helper injection and usage in controllers
- `HttpClientUtility` integration for external API calls
- XML documentation for new endpoints with DocFX alerts and code examples

### Updated
- `WFRunner.cs`:
	- Expanded workflow runner logic with advanced process handling
	- Enhanced dependency process and dynamic next process methods
	- Improved error handling and null safety
	- Added comprehensive XML documentation and remarks
- Controller constructor injection pattern updated to include new dependencies

### Enhanced
- Workflow process logic for MongoDB and PostgreSQL integration
- API response structure and null safety enforcement
- Documentation coverage for private/internal methods and new endpoints

---

## [v1.5.1](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/5b81d06888b4b111b1c87f2b4d7e27eb26bf52b0) - 2025-05-18

### Added
- `WFRunner` controller for workflow execution (`Controllers/WFRunner.cs`)
- MongoDB helpers and extensions: `Mongo.Commons.cs`, `Mongo.Extensions.cs`
- New MongoDB API endpoints in `TesterController.cs`
- Additional MongoDB connection strings and API URLs in `appsettings.json`

### Removed
- Unused file: `Controllers/Untitled-1.txt`

### Updated
- MongoDB workflow logic and null safety in `TesterController.cs`
- Exception handling improvements in `ExceptionExtension.cs`
- MongoDB helper and logging enhancements in `Mongo.Log.cs`, `Mongo.cs`, `Mongo.Const.cs`
- API configuration section expanded in `appsettings.json`

### Enhanced
- Workflow process and action checking logic for MongoDB
- XML documentation and code comments for new workflow features
- Null safety and coding standards enforced in new helpers and controllers
- Improved modularity and maintainability for MongoDB workflow operations

---

## [v1.5.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/1b4f5c1b1552bde7305b13234bfa611da2fb3fd0) - 2025-05-17

### Added
- MongoDB helper implementation and constants (`Utilities/Helpers/Data/Mongo.*.cs`)
- Common and Exception extension files (`Utilities/Extensions/CommonExtension.cs`, `Utilities/Extensions/ExceptionExtension.cs`)
- Centralized package management via `Directory.Packages.props`
- New MongoDB connection sample: `Controllers/Untitled-1.txt`
- MongoDB playground script: `tester.mongodb.js`

### Removed
- Legacy demo database file: `wwwroot/demo.dbml`
- IDC.Utilities DLL and PDB dependencies from `wwwroot/dependencies`

### Updated
- Refactored dependency injection setup for improved modularity (`Program.DI.Services.cs`, `Program.DI.cs`)
- Enhanced API key authentication, rate limiting, and response compression middleware
- Optimized model state invalid handling and request logging
- Swagger configuration updated for conflict resolution and endpoint exclusion
- Project references updated for new DLL paths and added `IDC.UDMongo` reference
- MongoDB connection strings and configuration in `appsettings.json` and `appconfigs.jsonc`
- XML documentation: replaced fully qualified type names with simplified references

### Enhanced
- Controller structure improved; `TesterController.cs` now supports MongoDB workflow endpoints
- Null safety and coding standards enforced in new helpers and extensions
- Swagger documentation now excludes endpoints from MongoDB reference DLLs
- Duration calculation utility added to `Commons.cs` for workflow reporting

---

## [v1.4.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/860c0477acc556e4e031c8a22a84d3655ff68e90) - 2025-04-29

### Added
- `.gitignore` file for bin, obj, logs, dependencies, and copilot-guidelines.txt
- New Swagger UI themes: monokai-dark, material, feeling-blue, flattop, muted, newspaper, outline
- New project file: `IDC.Template.csproj` (renamed from `IDC.UDMongo.csproj`)
- New HTTP test file: `IDC.Template.http`

### Removed
- `Controllers/MongoController.cs` (legacy MongoDB controller)

### Updated
- All namespaces and using directives from `IDC.UDMongo` to `IDC.Template`
- Project references: now uses local DLL reference for `IDC.Utilities`
- Swagger UI assets and configuration paths updated to `IDC.Template`
- License link in `README.md` now points to `IDC.Template/wwwroot/openapi/license.html`
- App name in `appsettings.json` and configuration handlers updated to `IDC.Template`
- XML documentation examples and code comments updated for new project name

### Enhanced
- Controller organization and modularity improved; all controllers now use `IDC.Template` namespace
- Middleware and DI setup files refactored for clarity and maintainability
- XML documentation expanded for Swagger filters and configuration handlers
- Changelog and documentation structure improved for easier navigation
- Null safety and coding standards enforced throughout controllers and utilities
- Swagger UI theme switching and grouping features updated for new structure

### Added
- `CHANGELOG.md` file for tracking version changes.
- `README.md` file with project information and usage instructions.

### Enhanced
- Documentation now includes project description, usage examples, and contact information.
- Changelog structure improved for easier readability and navigation.

---

## [v1.3.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/b158847c3b20b854b5f943fc683c235fbe84d9fd) - 2025-04-28

### Added
- New modular controllers:
  - `DemoController.Cache.cs` (cache endpoints)
  - `DemoController.ApiKey.cs` (API key endpoints)
  - `DemoController.Language.cs` (language endpoints)
  - `DemoController.SQLiteMemory.cs` (SQLite in-memory endpoints)
  - `DemoController.SystemLogging.cs` (system logging endpoints)
  - `MongoController.cs` (MongoDB endpoints)
  - `TesterController.cs` (simple test endpoint)
- New DI and middleware setup files:
  - `Program.DI.Core.cs`, `Program.DI.Services.cs`, `Program.DI.cs`
  - `Program.Middlewares.cs`, `Program.Services.cs`, `Program.Swagger.cs`
- New model: `Models/MongoDB/MongoDBTTableJoin.cs`
- New project file: `IDC.UDMongo.csproj`
- New HTTP test file: `IDC.Template.http`

### Removed
- Legacy controller: `Controllers/CouchbaseDemoController.cs`
- Legacy endpoint generator: `Program.EndPointGenerator.cs`
- `.gitignore` file

### Updated
- `DemoController.cs` now only contains a minimal stub, all features split into partial controllers
- Dependency injection and middleware setup refactored to new files for modularity
- Swagger configuration now supports grouping, sorting, and theme switching
- CORS, caching, logging, SQLite, and MongoDB DI setup now configuration-driven

### Enhanced
- All controllers now use primary constructor injection and follow project coding standards
- XML documentation improved for all new controllers and DI setup
- API responses standardized and null safety implemented throughout
- Middleware order and conditional enabling now strictly follow configuration
- Swagger UI supports runtime theme switching and external endpoint integration

---

## [v1.2.3](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/c21fe3970a9aaaf3cb889edb56ac80e46365fa36) - 2024-11-27

### Updated
- `IDX.Utilities.dll` and `IDX.Utilities.pdb` in `wwwroot/dependencies` updated to latest

---

## [v1.2.2](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/06c05b01a6d989fab5f210d39ee7a3e09fcb2449) - 2024-11-27

### Updated
- README.md: Added project description and .NET Core version

---

## [v1.2.1](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/76378384cceab2d22fc07e063f542a1f5f93209a) - 2024-11-27

### Updated
- Reordered using directives in `ModelStateInvalidFilters.cs` for clarity and consistency.
- Reformatted SQL queries in `CouchbaseDemoController.cs` to remove extra whitespace and improve readability.

### Enhanced
- Improved code style and maintainability by cleaning up unnecessary blank lines in SQL query constants.

---

## [v1.2.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/b1c44e9f4586fd3a9defb4ade876e046a3e11c3f) - 2024-11-27

### Added
- New endpoint for `DemoController`
- Solution file: `idc.template.sln`
- Default date/time format constants in `GeneralConstant`
- DocFX XML documentation improvements

### Updated
- Project name in `appsettings.json` and `Program.cs` from `idc.alert` to `idc.template`
- Logging configuration: log file path now defaults to `Logs.txt`
- Logging setup: added `WithSettingMinimumLevel`, `WithSettingsForceWriteToFile`, and `WithSettingsWriteToSystem` methods
- Null safety for configuration value reading in `Commons.cs`
- Language file loading logic in DI setup
- Couchbase and SQLite DI registration formatting

### Enhanced
- Exception handler: improved error response with stack trace in development mode
- Controller organization and primary constructor injection pattern
- API response serialization for error handling

### Removed
- Unused variable declarations in DI setup
- Legacy code and redundant comments

---

## [v1.1.0](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/f80597f2850d73c7ebc222eaad2b0bd6361f26a5) - 2024-11-27

### Added
- `.gitignore` for `obj/` and `bin/` folders
- Feature-based controllers: `DemoController`, `CouchbaseDemoController`
- Modular DI setup in `Program.DI.cs`
- Dynamic endpoint generator in `Program.EndPointGenerator.cs`
- Configuration utilities, constants, extensions, and validation helpers
- SQLite, PostgreSQL, Couchbase helpers and configuration
- Swagger UI theme switching and documentation improvements
- Language files for EN and ID
- Sample launch settings and configuration files
- DocFX-compatible XML documentation and code comments

### Updated
- README with .NET 8 note
- Solution renamed to `idc.template.sln`
- Dependency DLLs for IDX.Utilities

### Enhanced
- Null safety for configuration value reading
- Logging configuration and file path handling
- Controller organization and primary constructor injection pattern

### Removed
- Legacy code from previous template branch

---

## [v1.0.1](https://scm.idecision.ai/idecision_source_net8/idc.template/-/commit/36dc2b4f0670424e91313cbec57a9528b36d9cf4) - 2024-11-26

### Initial Release

- Project scaffolded as .NET 8.0 Web API template
- Modular dependency injection and configuration-driven middleware
- Partial program classes for setup and extensibility
- Feature-based controller organization
- Integrated IDC.Utilities external library
- Supports configuration via `appconfigs.jsonc`, `appsettings.json`, and dynamic endpoint definitions
- Build scripts for cross-platform development
- Swagger UI with runtime theme switching
- Automatic configuration












