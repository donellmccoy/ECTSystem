using System.Diagnostics;

namespace AF.ECT.Shared.Extensions;

/// <summary>
/// Extension methods for distributed tracing with W3C Trace Context support.
/// </summary>
/// <remarks>
/// Implements W3C Trace Context (https://www.w3.org/TR/trace-context/) for cross-service tracing.
/// Enables end-to-end tracing across client-server boundaries with proper parent-child relationships.
/// </remarks>
public static class DistributedTracingExtensions
{
    /// <summary>
    /// Injects W3C trace context into gRPC metadata headers for distributed tracing.
    /// </summary>
    /// <remarks>
    /// This method extracts trace IDs and span IDs from the current activity and
    /// propagates them via W3C standard headers (traceparent and tracestate).
    /// Server-side handlers should use ExtractW3CTraceContext to reconstruct the activity.
    /// </remarks>
    /// <param name="metadata">The gRPC metadata to inject trace context into.</param>
    /// <returns>The same metadata object for method chaining.</returns>
    public static Grpc.Core.Metadata InjectW3CTraceContext(this Grpc.Core.Metadata metadata)
    {
        var activity = Activity.Current;
        if (activity == null)
        {
            return metadata;
        }

        // W3C Trace Context format: traceparent = version-trace_id-parent_id-trace_flags
        var traceparent = $"00-{activity.TraceId:x}-{activity.SpanId:x}-{(activity.ActivityTraceFlags & ActivityTraceFlags.Recorded):x2}";
        metadata.Add("traceparent", traceparent);

        // Add tracestate if available
        if (!string.IsNullOrEmpty(activity.TraceStateString))
        {
            metadata.Add("tracestate", activity.TraceStateString);
        }

        // Add custom correlation ID if present
        if (activity.GetTagItem("correlation_id") is string correlationId)
        {
            metadata.Add("x-correlation-id", correlationId);
        }

        return metadata;
    }

    /// <summary>
    /// Extracts W3C trace context from gRPC metadata and sets it as the current activity.
    /// </summary>
    /// <remarks>
    /// This method reads W3C Trace Context headers from gRPC metadata and recreates
    /// the activity hierarchy on the server side. This enables seamless distributed tracing
    /// across the client-server boundary.
    /// </remarks>
    /// <param name="metadata">The gRPC metadata containing trace context headers.</param>
    /// <returns>An Activity representing the extracted trace context, or null if no trace context is present.</returns>
    public static Activity? ExtractW3CTraceContext(Grpc.Core.Metadata metadata)
    {
        var traceparent = metadata.FirstOrDefault(h => h.Key == "traceparent")?.Value;
        if (string.IsNullOrEmpty(traceparent))
        {
            return null;
        }

        try
        {
            // Parse W3C traceparent format: version-trace_id-parent_id-trace_flags
            var parts = traceparent.Split('-');
            if (parts.Length < 4)
            {
                return null;
            }

            var traceId = ActivityTraceId.CreateFromString(parts[1]);
            var spanId = ActivitySpanId.CreateFromString(parts[2]);
            var traceFlags = (ActivityTraceFlags)byte.Parse(parts[3], System.Globalization.NumberStyles.HexNumber);

            // Create a new activity linked to the parent context
            var activity = new Activity("grpc.server")
                .SetParentId(traceId, spanId, traceFlags);

            // Extract and restore tracestate
            var tracestate = metadata.FirstOrDefault(h => h.Key == "tracestate")?.Value;
            if (!string.IsNullOrEmpty(tracestate))
            {
                activity.TraceStateString = tracestate;
            }

            // Extract correlation ID
            var correlationId = metadata.FirstOrDefault(h => h.Key == "x-correlation-id")?.Value;
            if (!string.IsNullOrEmpty(correlationId))
            {
                activity.AddTag("correlation_id", correlationId);
            }

            return activity.Start();
        }
        catch (Exception ex)
        {
            // Log extraction failure but don't throw - tracing failure shouldn't break functionality
            System.Diagnostics.Debug.WriteLine($"Failed to extract W3C trace context: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Adds standard trace attributes to an activity for observability.
    /// </summary>
    /// <remarks>
    /// Sets conventional OpenTelemetry attributes for gRPC calls, enabling proper
    /// visualization and filtering in observability platforms.
    /// </remarks>
    /// <param name="activity">The activity to add attributes to.</param>
    /// <param name="methodName">The gRPC method name (e.g., "GetUserById").</param>
    /// <param name="serviceName">The service name (e.g., "WorkflowService").</param>
    /// <param name="correlationId">Optional correlation ID for linking with audit logs.</param>
    /// <returns>The same activity for method chaining.</returns>
    public static Activity? AddGrpcTraceAttributes(
        this Activity? activity,
        string methodName,
        string serviceName,
        string? correlationId = null)
    {
        if (activity == null)
        {
            return null;
        }

        activity.SetTag("rpc.method", methodName);
        activity.SetTag("rpc.service", serviceName);
        activity.SetTag("rpc.system", "grpc");
        activity.SetTag("service.name", "AF.ECT.Shared");

        if (!string.IsNullOrEmpty(correlationId))
        {
            activity.SetTag("correlation_id", correlationId);
        }

        return activity;
    }

    /// <summary>
    /// Sets performance metrics and status information on an activity.
    /// </summary>
    /// <remarks>
    /// Records success/failure status and performance timing for better observability
    /// and performance monitoring across the distributed system.
    /// </remarks>
    /// <param name="activity">The activity to update.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="durationMs">Duration of the operation in milliseconds.</param>
    /// <param name="errorMessage">Optional error message if operation failed.</param>
    /// <returns>The same activity for method chaining.</returns>
    public static Activity? RecordTraceMetrics(
        this Activity? activity,
        bool success,
        long durationMs,
        string? errorMessage = null)
    {
        if (activity == null)
        {
            return null;
        }

        activity.SetTag("operation.success", success);
        activity.SetTag("operation.duration_ms", durationMs);

        if (!success && !string.IsNullOrEmpty(errorMessage))
        {
            activity.SetTag("error.message", errorMessage);
        }

        return activity;
    }
}
