using AF.ECT.Shared.Extensions;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Unit tests for distributed tracing extensions.
/// </summary>
/// <remarks>
/// Verifies W3C Trace Context propagation for gRPC calls.
/// Tests trace ID injection, extraction, and attribute setting.
/// </remarks>
public class DistributedTracingTests
{
    [Fact]
    public void InjectW3CTraceContext_WithActivity_AddsMandatoryHeaders()
    {
        // Arrange
        var activity = new System.Diagnostics.Activity("test-operation").Start();
        var metadata = new Grpc.Core.Metadata();

        // Act
        metadata.InjectW3CTraceContext();

        // Assert
        metadata.Should().ContainSingle(h => h.Key == "traceparent");
    }

    [Fact]
    public void ExtractW3CTraceContext_ValidTraceparent_CreatesActivity()
    {
        // Arrange
        var metadata = new Grpc.Core.Metadata();
        var traceId = System.Diagnostics.ActivityTraceId.CreateRandom();
        var spanId = System.Diagnostics.ActivitySpanId.CreateRandom();
        var traceparent = $"00-{traceId:x}-{spanId:x}-01";
        metadata.Add("traceparent", traceparent);

        // Act
        var activity = DistributedTracingExtensions.ExtractW3CTraceContext(metadata);

        // Assert
        activity.Should().NotBeNull();
        activity?.TraceId.Should().Be(traceId);
    }

    [Fact]
    public void AddGrpcTraceAttributes_SetsCorrectOpenTelemetryAttributes()
    {
        // Arrange
        var activity = new System.Diagnostics.Activity("grpc-call").Start();

        // Act
        activity.AddGrpcTraceAttributes("GetUserById", "WorkflowService", "correlation-123");

        // Assert
        activity.GetTagItem("rpc.method").Should().Be("GetUserById");
        activity.GetTagItem("rpc.service").Should().Be("WorkflowService");
        activity.GetTagItem("correlation_id").Should().Be("correlation-123");
    }
}
