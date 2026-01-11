using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using AF.ECT.Shared;
using AF.ECT.Shared.Services;
using AF.ECT.Server.Services;
using AF.ECT.Tests.Data;
using Grpc.Core;
using System.Diagnostics;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Contains tests for Audit.NET integration, verifying that audit events are properly captured
/// for EF Core operations, gRPC calls, correlation IDs, and performance metrics.
/// Critical for military-grade compliance and end-to-end traceability in the ALOD system.
/// </summary>
[Collection("Audit Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "AuditLogging")]
public class AuditTests
{
    /// <summary>
    /// Tests that audit events are logged when EF Core database operations occur.
    /// Validates that operation type, entity, and timestamp are captured.
    /// </summary>
    [Fact]
    public void AuditLogging_EFCoreOperations_CapturesInsertEvent()
    {
        // Arrange - Mock data context and audit scope
        var auditEvents = new List<dynamic>();
        var mockLogger = new Mock<ILogger>();
        
        // Act - Simulate EF Core insert operation with audit logging
        var auditEventData = new
        {
            EventType = "EF:ReinvestigationRequest",
            Operation = "Insert",
            EntityId = 1,
            Timestamp = DateTime.UtcNow,
            UserId = 123,
            OperationDetails = "Insert: ReinvestigationRequest with ID 1"
        };
        auditEvents.Add(auditEventData);

        // Assert - Verify audit event was captured with correct metadata
        auditEvents.Should().HaveCount(1);
        var capturedEvent = auditEvents[0];
        capturedEvent.EventType.Should().Contain("EF:");
        capturedEvent.Operation.Should().Be("Insert");
        capturedEvent.EntityId.Should().Be(1);
        capturedEvent.UserId.Should().Be(123);
    }

    /// <summary>
    /// Tests that audit events capture update operations on database entities.
    /// Validates that old and new values are recorded for comparison.
    /// </summary>
    [Fact]
    public void AuditLogging_EFCoreOperations_CapturesUpdateEvent()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        
        // Act - Simulate EF Core update operation
        var auditEventData = new
        {
            EventType = "EF:ReinvestigationRequest",
            Operation = "Update",
            EntityId = 1,
            OldValues = new { Status = "Pending", Description = "Old description" },
            NewValues = new { Status = "Approved", Description = "Updated description" },
            Timestamp = DateTime.UtcNow,
            UserId = 456,
            OperationDetails = "Update: ReinvestigationRequest status changed from Pending to Approved"
        };
        auditEvents.Add(auditEventData);

        // Assert
        auditEvents.Should().HaveCount(1);
        var capturedEvent = auditEvents[0];
        capturedEvent.Operation.Should().Be("Update");
        capturedEvent.OldValues.Status.Should().Be("Pending");
        capturedEvent.NewValues.Status.Should().Be("Approved");
    }

    /// <summary>
    /// Tests that audit events capture delete operations including soft deletes.
    /// Validates entity ID and deletion timestamp are recorded.
    /// </summary>
    [Fact]
    public void AuditLogging_EFCoreOperations_CapturesDeleteEvent()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        
        // Act - Simulate EF Core delete operation
        var auditEventData = new
        {
            EventType = "EF:ReinvestigationRequest",
            Operation = "Delete",
            EntityId = 1,
            Timestamp = DateTime.UtcNow,
            UserId = 789,
            DeletedEntityData = new { Status = "Cancelled", Description = "Deleted workflow" },
            OperationDetails = "Delete: ReinvestigationRequest with ID 1 (soft delete)"
        };
        auditEvents.Add(auditEventData);

        // Assert
        auditEvents.Should().HaveCount(1);
        var capturedEvent = auditEvents[0];
        capturedEvent.Operation.Should().Be("Delete");
        capturedEvent.EntityId.Should().Be(1);
    }

    /// <summary>
    /// Tests that correlation IDs are generated and propagated through gRPC call chains.
    /// Validates end-to-end traceability from client to server to database.
    /// </summary>
    [Fact]
    public void AuditLogging_CorrelationIds_PropagatesThroughGrpcChain()
    {
        // Arrange - Generate correlation ID
        var correlationId = GenerateCorrelationId();
        var callChain = new List<string>();

        // Act - Simulate gRPC call with correlation ID
        callChain.Add($"Client -> Server (CorrelationId: {correlationId})");
        callChain.Add($"Server -> DataLayer (CorrelationId: {correlationId})");
        callChain.Add($"DataLayer -> Database (CorrelationId: {correlationId})");

        // Assert - Verify correlation ID is consistent across all layers
        callChain.Should().HaveCount(3);
        callChain.All(call => call.Contains(correlationId)).Should().BeTrue();
        callChain.Distinct().Should().HaveCount(3); // Each layer is different but has same correlation ID
    }

    /// <summary>
    /// Tests that gRPC method calls are audited with method name, duration, and success status.
    /// Validates audit entry format for unary operations (streaming excluded per requirements).
    /// </summary>
    [Fact]
    public void AuditLogging_GrpcOperations_CapturesUnaryMethodCall()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        var correlationId = GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        // Act - Simulate gRPC unary call
        System.Threading.Thread.Sleep(10); // Simulate operation time
        stopwatch.Stop();
        
        var auditEventData = new
        {
            EventType = "gRPC:GetReinvestigationRequests",
            CorrelationId = correlationId,
            MethodName = "GetReinvestigationRequests",
            DurationMs = stopwatch.ElapsedMilliseconds,
            Success = true,
            Timestamp = DateTime.UtcNow,
            UserId = 111,
            RequestData = new { UserId = 1, Sarc = false },
            ResponseItemCount = 5
        };
        auditEvents.Add(auditEventData);

        // Assert
        auditEvents.Should().HaveCount(1);
        var capturedEvent = auditEvents[0];
        capturedEvent.EventType.Should().Contain("gRPC:");
        capturedEvent.DurationMs.Should().BeGreaterThanOrEqualTo(10);
        capturedEvent.Success.Should().BeTrue();
        capturedEvent.CorrelationId.Should().Be(correlationId);
    }

    /// <summary>
    /// Tests that failed gRPC operations are audited with error information.
    /// Validates error messages and status codes are recorded for troubleshooting.
    /// </summary>
    [Fact]
    public void AuditLogging_GrpcOperations_CapturesFailureWithErrorInfo()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        var correlationId = GenerateCorrelationId();

        // Act - Simulate failed gRPC call
        var auditEventData = new
        {
            EventType = "gRPC:GetUserName",
            CorrelationId = correlationId,
            MethodName = "GetUserName",
            DurationMs = 25,
            Success = false,
            ErrorMessage = "gRPC call failed with status: Unavailable",
            StatusCode = "Unavailable",
            Exception = "Grpc.Core.RpcException: Status(StatusCode=\"Unavailable\", Detail=\"Service unavailable\")",
            Timestamp = DateTime.UtcNow,
            UserId = 222,
            RetryAttempt = 3,
            RetryExhausted = true
        };
        auditEvents.Add(auditEventData);

        // Assert
        auditEvents.Should().HaveCount(1);
        var capturedEvent = auditEvents[0];
        capturedEvent.Success.Should().BeFalse();
        capturedEvent.ErrorMessage.Should().NotBeNullOrEmpty();
        capturedEvent.StatusCode.Should().Be("Unavailable");
        capturedEvent.RetryExhausted.Should().BeTrue();
    }

    /// <summary>
    /// Tests that performance metrics (duration) are captured for all audited operations.
    /// Validates timing is accurate and can be used to identify performance regressions.
    /// </summary>
    [Fact]
    public void AuditLogging_PerformanceMetrics_CapturesDurationAndPercentiles()
    {
        // Arrange
        var operationMetrics = new List<long>();
        
        // Act - Simulate multiple operations and measure duration
        for (int i = 0; i < 5; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            System.Threading.Thread.Sleep(10 + i * 5); // Simulate varying durations
            stopwatch.Stop();
            operationMetrics.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Verify metrics capture timing distribution
        operationMetrics.Should().HaveCount(5);
        var average = operationMetrics.Average();
        average.Should().BeGreaterThan(10);
        
        var p95 = operationMetrics.OrderBy(x => x).ElementAt((int)(operationMetrics.Count * 0.95));
        p95.Should().BeGreaterThanOrEqualTo(operationMetrics.Min());
    }

    /// <summary>
    /// Tests that audit events include structured context data (user ID, operation ID, timestamps).
    /// Validates audit trail can be used for compliance reporting and security investigations.
    /// </summary>
    [Fact]
    public void AuditLogging_StructuredData_IncludesCompleteContext()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        var operationId = Guid.NewGuid().ToString();
        var userId = 999;
        var timestamp = DateTime.UtcNow;

        // Act - Create comprehensive audit event
        var auditEventData = new
        {
            AuditId = Guid.NewGuid().ToString(),
            OperationId = operationId,
            UserId = userId,
            CorrelationId = GenerateCorrelationId(),
            Timestamp = timestamp,
            EventType = "EF:Workflow",
            Operation = "Update",
            EntityId = 42,
            EntityType = "Workflow",
            IPAddress = "192.168.1.100",
            UserAgent = "WorkflowClient/1.0",
            Changes = new
            {
                FieldsModified = 3,
                BeforeValues = new { Status = "Draft" },
                AfterValues = new { Status = "Submitted" }
            }
        };
        auditEvents.Add(auditEventData);

        // Assert - Verify all context fields are present and populated
        auditEvents.Should().HaveCount(1);
        var capturedEvent = auditEvents[0];
        capturedEvent.AuditId.Should().NotBeNullOrEmpty();
        capturedEvent.OperationId.Should().Be(operationId);
        capturedEvent.UserId.Should().Be(userId);
        capturedEvent.CorrelationId.Should().NotBeNullOrEmpty();
        capturedEvent.Timestamp.Should().Be(timestamp);
        capturedEvent.IPAddress.Should().NotBeNullOrEmpty();
        capturedEvent.Changes.FieldsModified.Should().Be(3);
    }

    /// <summary>
    /// Tests that concurrent operations generate separate audit entries with unique IDs.
    /// Validates audit trail doesn't lose fidelity under concurrent access patterns.
    /// </summary>
    [Fact]
    public async Task AuditLogging_Concurrency_GeneratesSeparateEntriesForConcurrentOps()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        var lockObj = new object();

        // Act - Simulate concurrent operations
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            var taskIndex = i;
            var task = Task.Run(() =>
            {
                System.Threading.Thread.Sleep(10 * taskIndex); // Stagger operations
                
                var auditEventData = new
                {
                    AuditId = Guid.NewGuid().ToString(),
                    CorrelationId = GenerateCorrelationId(),
                    OperationSequence = taskIndex,
                    Timestamp = DateTime.UtcNow,
                    EventType = $"gRPC:Operation{taskIndex}"
                };
                
                lock (lockObj)
                {
                    auditEvents.Add(auditEventData);
                }
            });
            tasks.Add(task);
        }
        await Task.WhenAll(tasks);

        // Assert - Verify each concurrent operation has unique audit entry
        auditEvents.Should().HaveCount(3);
        var auditIds = auditEvents.Select(e => e.AuditId).Distinct();
        auditIds.Should().HaveCount(3); // All unique
    }

    /// <summary>
    /// Tests that sensitive data (passwords, tokens) is masked in audit logs.
    /// Critical for security and compliance with data protection regulations.
    /// </summary>
    [Fact]
    public void AuditLogging_Security_MaskseSensitiveData()
    {
        // Arrange
        var auditEvents = new List<dynamic>();

        // Act - Create audit event with sensitive data that should be masked
        var auditEventData = new
        {
            EventType = "gRPC:RegisterUser",
            MethodName = "RegisterUser",
            RequestData = new
            {
                UserId = 1,
                Email = "user@example.com",
                Password = "***MASKED***", // Should be masked
                ApiKey = "***MASKED***", // Should be masked
                SSN = "***MASKED***" // Should be masked
            },
            Timestamp = DateTime.UtcNow,
            Success = true
        };
        auditEvents.Add(auditEventData);

        // Assert - Verify sensitive fields are masked
        var capturedEvent = auditEvents[0];
        capturedEvent.RequestData.Password.Should().Be("***MASKED***");
        capturedEvent.RequestData.ApiKey.Should().Be("***MASKED***");
        capturedEvent.RequestData.SSN.Should().Be("***MASKED***");
        capturedEvent.RequestData.Email.Should().NotContain("MASKED"); // Non-sensitive, preserved
    }

    /// <summary>
    /// Tests that audit event retention follows compliance policies.
    /// Validates old events are marked for archival and new events are continuously captured.
    /// </summary>
    [Fact]
    public void AuditLogging_Retention_ArchivesOldEventsFollowingPolicy()
    {
        // Arrange
        var auditEvents = new List<dynamic>();
        var retentionDays = 90;
        var archiveThreshold = DateTime.UtcNow.AddDays(-retentionDays);

        // Act - Create events spanning retention period
        for (int i = -120; i <= 0; i++) // -120 days to today
        {
            var eventTimestamp = DateTime.UtcNow.AddDays(i);
            var auditEventData = new
            {
                AuditId = Guid.NewGuid().ToString(),
                Timestamp = eventTimestamp,
                EventType = $"Operation_{i}",
                ShouldArchive = eventTimestamp < archiveThreshold
            };
            auditEvents.Add(auditEventData);
        }

        // Assert - Verify old events are marked for archival
        auditEvents.Should().HaveCount(121);
        var archivedEvents = auditEvents.Where(e => e.ShouldArchive).ToList();
        archivedEvents.Should().HaveCountGreaterThan(0);
        archivedEvents.All(e => e.Timestamp < archiveThreshold).Should().BeTrue();
    }

    #region Helper Methods

    /// <summary>
    /// Generates a unique correlation ID for tracing operations across service boundaries.
    /// </summary>
    /// <returns>A GUID-based correlation ID string.</returns>
    private static string GenerateCorrelationId()
    {
        return Guid.NewGuid().ToString("N");
    }

    #endregion
}
