namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Extension methods for ITestOutputHelper to provide structured logging and diagnostics in tests.
/// Enables consistent, readable test output with performance metrics and audit trail logging.
/// </summary>
public static class TestOutputHelperExtensions
{
    /// <summary>
    /// Logs structured diagnostic information with category and timestamp.
    /// </summary>
    public static void LogStructured(
        this ITestOutputHelper output,
        string category,
        string message,
        Dictionary<string, object?>? contextData = null)
    {
        var timestamp = DateTime.UtcNow.ToString("O");
        var baseMessage = $"[{timestamp}] {category}: {message}";

        if (contextData is null || contextData.Count == 0)
        {
            output.WriteLine(baseMessage);
            return;
        }

        output.WriteLine(baseMessage);
        foreach (var kvp in contextData)
        {
            output.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }

    /// <summary>
    /// Logs a test step with sequential numbering and timestamp.
    /// </summary>
    public static void LogTestStep(
        this ITestOutputHelper output,
        int stepNumber,
        string stepDescription,
        string? expectedOutcome = null)
    {
        var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
        output.WriteLine($"[{timestamp}] STEP {stepNumber}: {stepDescription}");

        if (!string.IsNullOrEmpty(expectedOutcome))
            output.WriteLine($"  Expected: {expectedOutcome}");
    }

    /// <summary>
    /// Logs performance metrics for an operation with timing and resource usage.
    /// </summary>
    public static void LogPerformance(
        this ITestOutputHelper output,
        string operationName,
        TimeSpan duration,
        long? bytesAllocated = null,
        int? gcCollections = null)
    {
        var metrics = new Dictionary<string, object?>
        {
            { "Duration", $"{duration.TotalMilliseconds:F2}ms" },
            { "Seconds", $"{duration.TotalSeconds:F4}s" }
        };

        if (bytesAllocated.HasValue)
            metrics.Add("Bytes Allocated", FormatBytes(bytesAllocated.Value));

        if (gcCollections.HasValue)
            metrics.Add("GC Collections", gcCollections.Value);

        output.LogStructured("PERFORMANCE", operationName, metrics);
    }

    /// <summary>
    /// Logs an audit event with correlation ID and operation details.
    /// </summary>
    public static void LogAuditEvent(
        this ITestOutputHelper output,
        string operationName,
        string correlationId,
        bool success,
        TimeSpan duration,
        Dictionary<string, object?>? additionalContext = null)
    {
        var contextData = new Dictionary<string, object?>
        {
            { "CorrelationId", correlationId },
            { "Status", success ? "SUCCESS" : "FAILURE" },
            { "Duration", $"{duration.TotalMilliseconds:F2}ms" }
        };

        if (additionalContext != null)
        {
            foreach (var kvp in additionalContext)
            {
                contextData.Add(kvp.Key, kvp.Value);
            }
        }

        output.LogStructured("AUDIT", operationName, contextData);
    }

    /// <summary>
    /// Logs gRPC operation details including method, request/response types, and status.
    /// </summary>
    public static void LogGrpcOperation(
        this ITestOutputHelper output,
        string serviceName,
        string methodName,
        StatusCode statusCode,
        TimeSpan duration,
        string? errorMessage = null)
    {
        var contextData = new Dictionary<string, object?>
        {
            { "Service", serviceName },
            { "Method", methodName },
            { "Status", statusCode },
            { "Duration", $"{duration.TotalMilliseconds:F2}ms" }
        };

        if (!string.IsNullOrEmpty(errorMessage))
            contextData.Add("Error", errorMessage);

        output.LogStructured("gRPC", $"{serviceName}.{methodName}", contextData);
    }

    /// <summary>
    /// Logs database operation details including query type and execution time.
    /// </summary>
    public static void LogDatabaseOperation(
        this ITestOutputHelper output,
        string operationType,
        string? entityName = null,
        TimeSpan? duration = null,
        int? rowsAffected = null)
    {
        var contextData = new Dictionary<string, object?>
        {
            { "Type", operationType }
        };

        if (!string.IsNullOrEmpty(entityName))
            contextData.Add("Entity", entityName);

        if (duration.HasValue)
            contextData.Add("Duration", $"{duration.Value.TotalMilliseconds:F2}ms");

        if (rowsAffected.HasValue)
            contextData.Add("RowsAffected", rowsAffected.Value);

        output.LogStructured("DATABASE", operationType, contextData);
    }

    /// <summary>
    /// Logs a test assertion result with detailed context.
    /// </summary>
    public static void LogAssertion(
        this ITestOutputHelper output,
        string assertionDescription,
        bool passed,
        string? expectedValue = null,
        string? actualValue = null)
    {
        var status = passed ? "PASS" : "FAIL";
        var contextData = new Dictionary<string, object?>();

        if (!string.IsNullOrEmpty(expectedValue))
            contextData.Add("Expected", expectedValue);

        if (!string.IsNullOrEmpty(actualValue))
            contextData.Add("Actual", actualValue);

        var message = $"{status}: {assertionDescription}";
        output.LogStructured("ASSERTION", message, contextData.Count > 0 ? contextData : null);
    }

    /// <summary>
    /// Logs a warning or informational message.
    /// </summary>
    public static void LogWarning(
        this ITestOutputHelper output,
        string message,
        string? details = null)
    {
        var contextData = !string.IsNullOrEmpty(details) ? new Dictionary<string, object?> { { "Details", details } } : null;
        output.LogStructured("WARNING", message, contextData);
    }

    /// <summary>
    /// Logs an error or exception.
    /// </summary>
    public static void LogError(
        this ITestOutputHelper output,
        string message,
        Exception? exception = null)
    {
        var contextData = new Dictionary<string, object?>();

        if (exception != null)
        {
            contextData.Add("ExceptionType", exception.GetType().Name);
            contextData.Add("ExceptionMessage", exception.Message);

            if (!string.IsNullOrEmpty(exception.StackTrace))
                contextData.Add("StackTrace", exception.StackTrace);
        }

        output.LogStructured("ERROR", message, contextData.Count > 0 ? contextData : null);
    }

    /// <summary>
    /// Logs a detailed test summary with pass/fail counts and timing.
    /// </summary>
    public static void LogTestSummary(
        this ITestOutputHelper output,
        string testName,
        int totalAssertions,
        int passedAssertions,
        TimeSpan totalDuration)
    {
        var failedCount = totalAssertions - passedAssertions;
        var successRate = totalAssertions > 0 ? (passedAssertions * 100.0 / totalAssertions) : 0;

        var contextData = new Dictionary<string, object?>
        {
            { "Total Assertions", totalAssertions },
            { "Passed", passedAssertions },
            { "Failed", failedCount },
            { "Success Rate", $"{successRate:F1}%" },
            { "Total Duration", $"{totalDuration.TotalMilliseconds:F2}ms" }
        };

        output.LogStructured("TEST SUMMARY", testName, contextData);
    }

    /// <summary>
    /// Logs a section header to visually separate test phases.
    /// </summary>
    public static void LogSection(
        this ITestOutputHelper output,
        string sectionName)
    {
        var separator = new string('=', 60);
        output.WriteLine($"\n{separator}");
        output.WriteLine($"  {sectionName}");
        output.WriteLine($"{separator}\n");
    }

    /// <summary>
    /// Formats a byte count into human-readable format (B, KB, MB, GB).
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }
}
