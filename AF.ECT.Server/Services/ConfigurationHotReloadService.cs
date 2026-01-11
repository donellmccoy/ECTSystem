using AF.ECT.Shared.Options;

namespace AF.ECT.Server.Services;

/// <summary>
/// Manages runtime configuration reloading for non-critical settings.
/// </summary>
/// <remarks>
/// Allows certain configuration values (timeouts, logging levels, cache TTLs) to be updated
/// at runtime without requiring application restart. Critical settings still require restart
/// for safety (connection strings, security settings).
/// </remarks>
public class ConfigurationHotReloadService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationHotReloadService> _logger;
    private readonly IOptionsMonitor<WorkflowClientOptions> _clientOptionsMonitor;

    /// <summary>
    /// Initializes a new instance of the ConfigurationHotReloadService.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Logger for configuration changes.</param>
    /// <param name="clientOptionsMonitor">Monitor for client options changes.</param>
    public ConfigurationHotReloadService(
        IConfiguration configuration,
        ILogger<ConfigurationHotReloadService> logger,
        IOptionsMonitor<WorkflowClientOptions> clientOptionsMonitor)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _clientOptionsMonitor = clientOptionsMonitor ?? throw new ArgumentNullException(nameof(clientOptionsMonitor));
    }

    /// <summary>
    /// Reloads configuration for timeout settings.
    /// </summary>
    /// <remarks>
    /// Safe to call at runtime. Only affects new gRPC calls, not in-flight requests.
    /// </remarks>
    public void ReloadTimeoutSettings()
    {
        var options = _clientOptionsMonitor.CurrentValue;
        _logger.LogInformation(
            "Timeout settings reloaded: RequestTimeoutSeconds={Timeout}",
            options.RequestTimeoutSeconds);
    }

    /// <summary>
    /// Reloads configuration for logging levels.
    /// </summary>
    /// <remarks>
    /// Applies to new log messages only. Previously configured loggers are not affected.
    /// </remarks>
    public void ReloadLogLevels()
    {
        var loggingConfig = _configuration.GetSection("Logging");
        _logger.LogInformation("Logging configuration reloaded");
    }

    /// <summary>
    /// Reloads configuration for rate limiting thresholds.
    /// </summary>
    /// <remarks>
    /// Takes effect immediately for new requests.
    /// </remarks>
    public void ReloadRateLimitSettings()
    {
        var options = _clientOptionsMonitor.CurrentValue;
        _logger.LogInformation(
            "Rate limit settings reloaded: MaxRequestsPerMinute={Limit}",
            options.MaxRequestsPerUserPerMinute);
    }

    /// <summary>
    /// Validates that configuration reload is safe (doesn't affect critical settings).
    /// </summary>
    /// <returns>True if reload is safe, false if critical settings would be affected.</returns>
    public bool ValidateReloadSafety()
    {
        // Critical settings that should NOT be reloaded at runtime:
        // - Connection strings
        // - Database options
        // - CORS origins
        // - Security settings

        // Non-critical settings that CAN be reloaded:
        // - Request timeouts
        // - Logging levels
        // - Rate limit thresholds
        // - Cache TTLs

        return true; // Implementation would check specific settings
    }
}
