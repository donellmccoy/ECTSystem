using AF.ECT.Server.Services.Interfaces;

namespace AF.ECT.Server.Services;

/// <summary>
/// Provides correlation ID management for distributed tracing across requests.
/// </summary>
/// <remarks>
/// Generates and manages correlation IDs that flow through requests, enabling
/// end-to-end tracing for debugging, performance monitoring, and audit logging.
/// </remarks>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdContextKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CorrelationIdProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the CorrelationIdProvider.
    /// </summary>
    /// <param name="httpContextAccessor">Accessor for current HTTP context.</param>
    /// <param name="logger">Logger for correlation ID operations.</param>
    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor, ILogger<CorrelationIdProvider> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current correlation ID, creating a new one if necessary.
    /// </summary>
    /// <returns>The current correlation ID.</returns>
    public string GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return GenerateCorrelationId();
        }

        // Try to get from context items first
        if (context.Items.TryGetValue(CorrelationIdContextKey, out var correlationId) &&
            correlationId is string id && !string.IsNullOrWhiteSpace(id))
        {
            return id;
        }

        // Try to get from request headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValues) &&
            headerValues.Count > 0 && !string.IsNullOrWhiteSpace(headerValues[0]))
        {
            var headerId = headerValues[0]!;
            SetCorrelationId(headerId);
            return headerId;
        }

        // Generate new correlation ID
        var newId = GenerateCorrelationId();
        SetCorrelationId(newId);
        return newId;
    }

    /// <summary>
    /// Sets the correlation ID for the current request context.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    public void SetCorrelationId(string correlationId)
    {
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return;
        }

        context.Items[CorrelationIdContextKey] = correlationId;

        // Also set response header for client-side tracing
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
        }
    }

    /// <summary>
    /// Generates a new unique correlation ID.
    /// </summary>
    /// <returns>A new correlation ID.</returns>
    public string GenerateCorrelationId()
    {
        return $"{Environment.MachineName}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
