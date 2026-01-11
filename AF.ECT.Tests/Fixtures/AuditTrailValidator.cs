namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Helper for validating audit trail entries in tests.
/// Verifies audit.net audit logs were created with correct metadata, timestamps, and operation results.
/// </summary>
public static class AuditTrailValidator
{
    /// <summary>
    /// Represents expected audit event characteristics.
    /// </summary>
    public class AuditEventSpec
    {
        /// <summary>
        /// Gets or sets the expected event type.
        /// </summary>
        public string? ExpectedEventType { get; set; }

        /// <summary>
        /// Gets or sets whether operation should be successful.
        /// </summary>
        public bool ExpectSuccess { get; set; } = true;

        /// <summary>
        /// Gets or sets the expected correlation ID.
        /// </summary>
        public string? ExpectedCorrelationId { get; set; }

        /// <summary>
        /// Gets or sets minimum expected duration in milliseconds.
        /// </summary>
        public int? MinDurationMs { get; set; }

        /// <summary>
        /// Gets or sets maximum expected duration in milliseconds.
        /// </summary>
        public int? MaxDurationMs { get; set; }

        /// <summary>
        /// Gets or sets whether exception information should be present.
        /// </summary>
        public bool ExpectException { get; set; }

        /// <summary>
        /// Gets or sets expected exception type name.
        /// </summary>
        public string? ExpectExceptionType { get; set; }

        /// <summary>
        /// Gets or sets additional context properties to validate.
        /// </summary>
        public Dictionary<string, object?>? ExpectContextProperties { get; set; }
    }

    /// <summary>
    /// Represents a single audit event entry.
    /// </summary>
    public class AuditEventEntry
    {
        /// <summary>
        /// Gets the event type.
        /// </summary>
        public string EventType { get; init; } = "";

        /// <summary>
        /// Gets the timestamp when event occurred.
        /// </summary>
        public DateTime Timestamp { get; init; }

        /// <summary>
        /// Gets the correlation ID linking related operations.
        /// </summary>
        public string CorrelationId { get; init; } = "";

        /// <summary>
        /// Gets the operation name that triggered the audit.
        /// </summary>
        public string OperationName { get; init; } = "";

        /// <summary>
        /// Gets whether operation completed successfully.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Gets the operation duration in milliseconds.
        /// </summary>
        public long DurationMs { get; init; }

        /// <summary>
        /// Gets exception information if operation failed.
        /// </summary>
        public ExceptionInfo? Exception { get; init; }

        /// <summary>
        /// Gets additional context data from the audit entry.
        /// </summary>
        public Dictionary<string, object?> ContextData { get; } = [];
    }

    /// <summary>
    /// Represents exception details in an audit entry.
    /// </summary>
    public class ExceptionInfo
    {
        /// <summary>
        /// Gets the exception type name.
        /// </summary>
        public string TypeName { get; init; } = "";

        /// <summary>
        /// Gets the exception message.
        /// </summary>
        public string Message { get; init; } = "";

        /// <summary>
        /// Gets the exception stack trace.
        /// </summary>
        public string? StackTrace { get; init; }
    }

    /// <summary>
    /// Validates an individual audit event against expected specifications.
    /// </summary>
    public static void ValidateAuditEvent(AuditEventEntry entry, AuditEventSpec spec)
    {
        if (!string.IsNullOrEmpty(spec.ExpectedEventType))
        {
            entry.EventType.Should().Contain(spec.ExpectedEventType,
                because: $"event type should contain '{spec.ExpectedEventType}'");
        }

        entry.Success.Should().Be(spec.ExpectSuccess,
            because: $"event success status should be {spec.ExpectSuccess}");

        if (!string.IsNullOrEmpty(spec.ExpectedCorrelationId))
        {
            entry.CorrelationId.Should().Be(spec.ExpectedCorrelationId,
                because: "correlation ID should match expected value");
        }

        if (spec.MinDurationMs.HasValue)
        {
            entry.DurationMs.Should().BeGreaterThanOrEqualTo(spec.MinDurationMs.Value,
                because: $"operation should take at least {spec.MinDurationMs}ms");
        }

        if (spec.MaxDurationMs.HasValue)
        {
            entry.DurationMs.Should().BeLessThanOrEqualTo(spec.MaxDurationMs.Value,
                because: $"operation should not exceed {spec.MaxDurationMs}ms");
        }

        if (spec.ExpectException)
        {
            entry.Exception.Should().NotBeNull(because: "exception information should be present");

            if (!string.IsNullOrEmpty(spec.ExpectExceptionType))
            {
                entry.Exception!.TypeName.Should().Be(spec.ExpectExceptionType,
                    because: $"exception type should be {spec.ExpectExceptionType}");
            }
        }
        else
        {
            entry.Exception.Should().BeNull(because: "no exception should be present");
        }

        if (spec.ExpectContextProperties != null)
        {
            foreach (var kvp in spec.ExpectContextProperties)
            {
                entry.ContextData.Should().ContainKey(kvp.Key,
                    because: $"context should contain '{kvp.Key}' property");

                entry.ContextData[kvp.Key].Should().Be(kvp.Value,
                    because: $"'{kvp.Key}' context value should match");
            }
        }
    }

    /// <summary>
    /// Validates a sequence of audit events to ensure proper operation flow.
    /// </summary>
    public static void ValidateAuditEventSequence(
        IEnumerable<AuditEventEntry> events,
        params AuditEventSpec[] expectedSequence)
    {
        var eventList = events.ToList();
        eventList.Should().HaveCount(expectedSequence.Length,
            because: $"expected {expectedSequence.Length} audit events but found {eventList.Count}");

        for (int i = 0; i < expectedSequence.Length; i++)
        {
            ValidateAuditEvent(eventList[i], expectedSequence[i]);
        }
    }

    /// <summary>
    /// Validates that all events in a sequence share the same correlation ID.
    /// </summary>
    public static void ValidateCorrelationIdConsistency(IEnumerable<AuditEventEntry> events)
    {
        var eventList = events.ToList();
        eventList.Should().NotBeEmpty(because: "events list should not be empty");

        var firstCorrelationId = eventList.First().CorrelationId;
        firstCorrelationId.Should().NotBeEmpty(because: "correlation ID should be present");

        eventList.Should().AllSatisfy(e =>
        {
            e.CorrelationId.Should().Be(firstCorrelationId,
                because: "all events should share the same correlation ID");
        });
    }

    /// <summary>
    /// Validates that audit events maintain chronological order.
    /// </summary>
    public static void ValidateTimestampOrdering(IEnumerable<AuditEventEntry> events)
    {
        var eventList = events.ToList();

        for (int i = 1; i < eventList.Count; i++)
        {
            eventList[i].Timestamp.Should().BeOnOrAfter(eventList[i - 1].Timestamp,
                because: "events should be in chronological order");
        }
    }

    /// <summary>
    /// Validates gRPC-specific audit event patterns.
    /// </summary>
    public static void ValidateGrpcAuditEvent(
        AuditEventEntry entry,
        string expectedService,
        string expectedMethod,
        bool expectSuccess = true)
    {
        entry.EventType.Should().Contain("gRPC",
            because: "event type should indicate gRPC operation");

        entry.OperationName.Should().Contain(expectedService,
            because: $"operation should reference service '{expectedService}'");

        entry.OperationName.Should().Contain(expectedMethod,
            because: $"operation should reference method '{expectedMethod}'");

        entry.Success.Should().Be(expectSuccess,
            because: $"gRPC operation success should be {expectSuccess}");
    }

    /// <summary>
    /// Validates Entity Framework audit event patterns.
    /// </summary>
    public static void ValidateEFAuditEvent(
        AuditEventEntry entry,
        string expectedEntityType,
        string expectedOperationType)
    {
        entry.EventType.Should().Contain("EF",
            because: "event type should indicate Entity Framework operation");

        entry.EventType.Should().Contain(expectedEntityType,
            because: $"event type should reference entity '{expectedEntityType}'");

        entry.OperationName.Should().Contain(expectedOperationType,
            because: $"operation should be '{expectedOperationType}'");
    }

    /// <summary>
    /// Creates an audit event spec for successful gRPC operations.
    /// </summary>
    public static AuditEventSpec GrpcSuccessSpec(
        string service,
        string method,
        string correlationId,
        int? maxDurationMs = 5000) => new()
    {
        ExpectedEventType = "gRPC",
        ExpectSuccess = true,
        ExpectedCorrelationId = correlationId,
        ExpectException = false,
        MaxDurationMs = maxDurationMs,
        ExpectContextProperties = new()
        {
            { "Service", service },
            { "Method", method }
        }
    };

    /// <summary>
    /// Creates an audit event spec for failed gRPC operations.
    /// </summary>
    public static AuditEventSpec GrpcFailureSpec(
        string service,
        string method,
        string correlationId,
        string expectedExceptionType = "RpcException") => new()
    {
        ExpectedEventType = "gRPC",
        ExpectSuccess = false,
        ExpectedCorrelationId = correlationId,
        ExpectException = true,
        ExpectExceptionType = expectedExceptionType,
        ExpectContextProperties = new()
        {
            { "Service", service },
            { "Method", method }
        }
    };

    /// <summary>
    /// Creates an audit event spec for Entity Framework operations.
    /// </summary>
    public static AuditEventSpec EFSpec(
        string entityType,
        string operationType,
        string correlationId,
        int? maxDurationMs = 1000) => new()
    {
        ExpectedEventType = "EF",
        ExpectSuccess = true,
        ExpectedCorrelationId = correlationId,
        MaxDurationMs = maxDurationMs,
        ExpectContextProperties = new()
        {
            { "EntityType", entityType },
            { "OperationType", operationType }
        }
    };

    /// <summary>
    /// Formats audit events for test output/logging.
    /// </summary>
    public static string FormatAuditEvents(IEnumerable<AuditEventEntry> events)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Audit Trail:");
        sb.AppendLine(new string('=', 60));

        foreach (var evt in events)
        {
            sb.AppendLine($"[{evt.Timestamp:O}] {evt.EventType}");
            sb.AppendLine($"  Correlation: {evt.CorrelationId}");
            sb.AppendLine($"  Operation: {evt.OperationName}");
            sb.AppendLine($"  Success: {evt.Success}");
            sb.AppendLine($"  Duration: {evt.DurationMs}ms");

            if (evt.Exception != null)
            {
                sb.AppendLine($"  Exception: {evt.Exception.TypeName}");
                sb.AppendLine($"    Message: {evt.Exception.Message}");
            }

            if (evt.ContextData.Count > 0)
            {
                sb.AppendLine("  Context:");
                foreach (var kvp in evt.ContextData)
                {
                    sb.AppendLine($"    {kvp.Key}: {kvp.Value}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
