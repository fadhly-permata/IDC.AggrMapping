namespace IDC.AggrMapping.Utilities.Models;

/// <summary>
/// Represents a request model for generating a user-specific API key.
/// </summary>
/// <remarks>
/// This record is used to encapsulate the necessary information for generating an API key tied to a specific user.
/// The generated key will be valid until the specified expiry date.
///
/// Example usage:
/// <example>
/// <code>
/// var request = new UserApiKeyRequest(
///     userId: "user123",
///     appId: "app456",
///     expiryDate: DateTime.UtcNow.AddDays(30)
/// );
/// </code>
/// </example>
///
/// > [!IMPORTANT]
/// > Always use UTC time for ExpiryDate to avoid timezone issues
///
/// > [!NOTE]
/// > The AppId should match a registered application in the system
/// </remarks>
/// <param name="UserId">
/// The unique identifier of the user.
/// <see href="https://learn.microsoft.com/en-us/windows/win32/secauthn/user-name-formats">User ID Format Reference</see>
/// </param>
/// <param name="AppId">
/// The application identifier associated with the API key.
/// <see href="https://learn.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals">Application ID Reference</see>
/// </param>
/// <param name="ExpiryDate">
/// The date and time when the API key will expire.
/// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.datetime">DateTime Reference</see>
/// </param>
/// <returns>A new instance of UserApiKeyRequest with the specified parameters.</returns>
/// <exception cref="ArgumentNullException">Thrown when UserId or AppId is null.</exception>
/// <exception cref="ArgumentException">Thrown when ExpiryDate is in the past.</exception>
public record UserApiKeyRequest(string UserId, string AppId, DateTime ExpiryDate);

/// <summary>
/// Represents a request model for generating a temporary API key with specified validity duration.
/// </summary>
/// <remarks>
/// This record encapsulates the necessary information for generating a time-limited API key for temporary access.
/// The generated key will automatically expire after the specified validity period.
///
/// Example usage:
/// <example>
/// <code>
/// var request = new TemporaryApiKeyRequest(
///     validity: TimeSpan.FromHours(24),
///     purpose: "temporary-access-deployment"
/// );
/// </code>
/// </example>
///
/// Example request body:
/// <code>
/// {
///     "validity": "24:00:00",
///     "purpose": "temporary-access-deployment"
/// }
/// </code>
///
/// > [!IMPORTANT]
/// > Validity duration should not exceed maximum allowed period (typically 72 hours)
///
/// > [!TIP]
/// > Use descriptive purpose strings to easily track API key usage
/// </remarks>
/// <param name="Validity">
/// The duration for which the temporary API key will be valid.
/// <see href="https://learn.microsoft.com/en-us/dotnet/api/system.timespan">TimeSpan Reference</see>
/// </param>
/// <param name="Purpose">
/// The intended purpose or use case for the temporary API key.
/// <see href="https://learn.microsoft.com/en-us/azure/architecture/patterns/valet-key">Valet Key Pattern</see>
/// </param>
/// <returns>A new instance of TemporaryApiKeyRequest with the specified parameters.</returns>
/// <exception cref="ArgumentException">Thrown when Validity is negative or exceeds maximum allowed duration.</exception>
/// <exception cref="ArgumentNullException">Thrown when Purpose is null or empty.</exception>
public record TemporaryApiKeyRequest(TimeSpan Validity, string Purpose);

/// <summary>
/// Represents a request model for generating a client-specific API key with defined permissions.
/// </summary>
/// <remarks>
/// This record encapsulates the necessary information for generating an API key tied to a specific client application.
/// The generated key will include the specified permissions for access control.
///
/// Example usage:
/// <example>
/// <code>
/// var request = new ClientApiKeyRequest(
///     clientId: "client_123",
///     clientSecret: "secret_xyz",
///     permissions: new[] { "read:users", "write:reports", "admin:settings" }
/// );
/// </code>
/// </example>
///
/// Example request body:
/// <code>
/// {
///     "clientId": "client_123",
///     "clientSecret": "secret_xyz",
///     "permissions": [
///         "read:users",
///         "write:reports",
///         "admin:settings"
///     ]
/// }
/// </code>
///
/// > [!IMPORTANT]
/// > Client secrets should be stored securely and never exposed in logs or client-side code
///
/// > [!NOTE]
/// > Permissions should follow the format: "action:resource"
/// </remarks>
/// <param name="ClientId">
/// The unique identifier of the client application.
/// <see href="https://auth0.com/docs/get-started/applications/application-settings#application-properties">Client ID Format Reference</see>
/// </param>
/// <param name="ClientSecret">
/// The secret key associated with the client for authentication.
/// <see href="https://auth0.com/docs/secure/tokens/refresh-tokens/security-best-practices">Client Secret Best Practices</see>
/// </param>
/// <param name="Permissions">
/// Array of permission strings defining the access scope.
/// <see href="https://auth0.com/docs/manage-users/access-control/rbac">RBAC Permission Format</see>
/// </param>
/// <returns>A new instance of ClientApiKeyRequest with the specified parameters.</returns>
/// <exception cref="ArgumentNullException">Thrown when ClientId, ClientSecret, or Permissions is null.</exception>
/// <exception cref="ArgumentException">Thrown when Permissions array is empty or contains invalid permission formats.</exception>
public record ClientApiKeyRequest(string ClientId, string ClientSecret, string[] Permissions);

/// <summary>
/// Represents a request for generating an environment-specific API key.
/// </summary>
/// <remarks>
/// This record encapsulates the necessary information for generating an API key specific to a deployment environment.
/// Used to manage access control across different deployment stages and service versions.
///
/// Example usage:
/// <example>
/// <code>
/// var request = new EnvironmentApiKeyRequest(
///     environment: "production",
///     serviceName: "payment-gateway",
///     version: "v2.1.0"
/// );
/// </code>
/// </example>
///
/// > [!IMPORTANT]
/// > Environment names should follow standard naming conventions (development, staging, production)
///
/// > [!TIP]
/// > Use semantic versioning format for version strings (e.g., v1.0.0)
/// </remarks>
/// <param name="Environment">
/// The target environment identifier.
/// <see href="https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/considerations/environments">Environment Types Reference</see>
/// </param>
/// <param name="ServiceName">
/// The name of the service requiring the API key.
/// <see href="https://learn.microsoft.com/en-us/azure/architecture/best-practices/naming-conventions">Service Naming Conventions</see>
/// </param>
/// <param name="Version">
/// The version identifier of the service.
/// <see href="https://semver.org/">Semantic Versioning Reference</see>
/// </param>
/// <returns>A new instance of EnvironmentApiKeyRequest with the specified parameters.</returns>
/// <exception cref="ArgumentNullException">Thrown when any parameter is null or empty.</exception>
/// <exception cref="ArgumentException">Thrown when environment name is invalid or version format is incorrect.</exception>
public record EnvironmentApiKeyRequest(string Environment, string ServiceName, string Version);
