namespace AF.ECT.Tests.Common;

/// <summary>
/// Provides shared test fixtures using xUnit's Collection Fixtures.
/// Reduces setup/teardown overhead for expensive operations.
/// </summary>
[CollectionDefinition("Workflow Service Tests", DisableParallelization = false)]
public class WorkflowServiceTestCollection
{
    // This class has no code, just used to define the collection
}

[CollectionDefinition("Integration Tests", DisableParallelization = false)]
public class IntegrationTestsCollection
{
    // This class has no code, just used to define the collection
}

[CollectionDefinition("Streaming Tests", DisableParallelization = false)]
public class StreamingTestsCollection
{
    // This class has no code, just used to define the collection
}

/// <summary>
/// Fixture for managing audit log verification across tests.
/// Tracks and validates audit events and correlation IDs.
/// </summary>
public class AuditTrailFixture : IAsyncLifetime
{
    private readonly List<AuditLogEntry> _capturedEvents = new();

    /// <summary>
    /// Gets the captured audit events.
    /// </summary>
    public IReadOnlyList<AuditLogEntry> CapturedEvents => _capturedEvents.AsReadOnly();

    /// <summary>
    /// Audit log entry representation.
    /// </summary>
    public class AuditLogEntry
    {
        /// <summary>
        /// Gets or sets the correlation ID.
        /// </summary>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the event type.
        /// </summary>
        public string? EventType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the duration in milliseconds.
        /// </summary>
        public long DurationMs { get; set; }

        /// <summary>
        /// Gets or sets whether the operation succeeded.
        /// </summary>
        public bool Success { get; set; }
    }

    /// <summary>
    /// Initializes the audit trail fixture.
    /// </summary>
    public Task InitializeAsync()
    {
        _capturedEvents.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cleans up captured events.
    /// </summary>
    public Task DisposeAsync()
    {
        _capturedEvents.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Records an audit event.
    /// </summary>
    /// <param name="entry">The audit entry to record</param>
    public void RecordEvent(AuditLogEntry entry)
    {
        _capturedEvents.Add(entry);
    }

    /// <summary>
    /// Finds audit events by correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID to search for</param>
    /// <returns>Matching audit events</returns>
    public IEnumerable<AuditLogEntry> GetByCorrelationId(string correlationId) =>
        _capturedEvents.Where(e => e.CorrelationId == correlationId);

    /// <summary>
    /// Finds audit events by event type.
    /// </summary>
    /// <param name="eventType">The event type to search for</param>
    /// <returns>Matching audit events</returns>
    public IEnumerable<AuditLogEntry> GetByEventType(string eventType) =>
        _capturedEvents.Where(e => e.EventType == eventType);
}
