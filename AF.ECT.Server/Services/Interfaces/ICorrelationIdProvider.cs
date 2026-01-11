namespace AF.ECT.Server.Services.Interfaces;

/// <summary>
/// Provides correlation ID management for distributed tracing across requests.
/// </summary>
/// <remarks>
/// Correlation IDs enable end-to-end tracing of requests through multiple services
/// for debugging, auditing, and performance monitoring.
/// </remarks>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Gets the current correlation ID, creating a new one if necessary.
    /// </summary>
    /// <returns>The current correlation ID.</returns>
    string GetCorrelationId();

    /// <summary>
    /// Sets the correlation ID for the current request context.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Generates a new unique correlation ID.
    /// </summary>
    /// <returns>A new correlation ID.</returns>
    string GenerateCorrelationId();
}
