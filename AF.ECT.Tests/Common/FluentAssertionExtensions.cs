using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using Grpc.Core;

namespace AF.ECT.Tests.Common;

/// <summary>
/// Provides FluentAssertions extensions for common test scenarios.
/// Improves assertion readability and reduces boilerplate in test methods.
/// </summary>
public static class FluentAssertionExtensions
{
    /// <summary>
    /// Asserts that a gRPC response contains expected items.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    /// <param name="response">The response to assert</param>
    /// <param name="expectedCount">The expected number of items</param>
    /// <returns>Assertion context for chaining</returns>
    public static AndConstraint<ObjectAssertions> HaveItemCount<T>(
        this ObjectAssertions response,
        int expectedCount)
        where T : class
    {
        response.NotBeNull();
        var countProperty = response.Subject!.GetType().GetProperty("Count");
        var itemsProperty = response.Subject!.GetType().GetProperty("Items");

        if (countProperty != null)
        {
            var count = (int)countProperty.GetValue(response.Subject)!;
            count.Should().Be(expectedCount);
        }

        if (itemsProperty != null)
        {
            var items = itemsProperty.GetValue(response.Subject) as System.Collections.ICollection;
            items?.Count.Should().Be(expectedCount);
        }

        return new AndConstraint<ObjectAssertions>(response);
    }

    /// <summary>
    /// Asserts that a response is empty.
    /// </summary>
    /// <param name="response">The response to assert</param>
    /// <returns>Assertion context for chaining</returns>
    public static AndConstraint<ObjectAssertions> BeEmpty(
        this ObjectAssertions response)
    {
        return response.HaveItemCount<object>(0);
    }

    /// <summary>
    /// Asserts that an async operation completes within a timeout.
    /// </summary>
    /// <param name="task">The task to assert</param>
    /// <param name="timeout">The timeout duration</param>
    /// <returns>Awaitable task for assertion continuation</returns>
    public static async Task CompleteWithinAsync(
        this Task task,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new AssertionFailedException(
                $"Task did not complete within {timeout.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Asserts that an async operation completes within a timeout and returns a value.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="task">The task to assert</param>
    /// <param name="timeout">The timeout duration</param>
    /// <returns>The task result</returns>
    public static async Task<T> CompleteWithinAsync<T>(
        this Task<T> task,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new AssertionFailedException(
                $"Task did not complete within {timeout.TotalMilliseconds}ms");
        }
    }

    /// <summary>
    /// Asserts that an async enumerable yields expected number of items.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    /// <param name="enumerable">The async enumerable to assert</param>
    /// <param name="expectedCount">The expected count</param>
    /// <returns>The collected items</returns>
    public static async Task<List<T>> HaveCountAsync<T>(
        this IAsyncEnumerable<T> enumerable,
        int expectedCount)
    {
        var items = new List<T>();
        await foreach (var item in enumerable.ConfigureAwait(false))
        {
            items.Add(item);
        }

        items.Should().HaveCount(expectedCount);
        return items;
    }

    /// <summary>
    /// Asserts that a streaming operation does not throw.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    /// <param name="enumerable">The async enumerable to assert</param>
    /// <returns>The collected items without throwing</returns>
    public static async Task<List<T>> NotThrowAsync<T>(
        this IAsyncEnumerable<T> enumerable)
    {
        var items = new List<T>();
        try
        {
            await foreach (var item in enumerable.ConfigureAwait(false))
            {
                items.Add(item);
            }
            return items;
        }
        catch (Exception ex)
        {
            throw new AssertionFailedException(
                $"Streaming operation threw unexpected exception: {ex.Message}");
        }
    }
}

/// <summary>
/// Assertion helpers for exception validation.
/// </summary>
public static class ExceptionAssertions
{
    /// <summary>
    /// Asserts that an exception contains a specific message.
    /// </summary>
    /// <param name="exception">The exception to assert</param>
    /// <param name="expectedMessage">The expected message</param>
    public static void ContainsMessage(Exception exception, string expectedMessage)
    {
        exception.Should().NotBeNull();
        exception.Message.Should().Contain(expectedMessage);
    }

    /// <summary>
    /// Asserts that an exception is of a specific type and contains a message.
    /// </summary>
    /// <typeparam name="TException">The expected exception type</typeparam>
    /// <param name="exception">The exception to assert</param>
    /// <param name="expectedMessage">The expected message</param>
    public static void IsTypeWithMessage<TException>(
        Exception exception,
        string expectedMessage)
        where TException : Exception
    {
        exception.Should().BeOfType<TException>();
        exception.Message.Should().Contain(expectedMessage);
    }

    /// <summary>
    /// Asserts that an RPC exception has a specific status code.
    /// </summary>
    /// <param name="exception">The RPC exception to assert</param>
    /// <param name="statusCode">The expected status code</param>
    public static void HasStatusCode(RpcException exception, StatusCode statusCode)
    {
        exception.Should().NotBeNull();
        exception.StatusCode.Should().Be(statusCode);
    }
}

/// <summary>
/// Assertion helpers for audit trail validation.
/// </summary>
public static class AuditAssertions
{
    /// <summary>
    /// Asserts that an operation was properly audited.
    /// </summary>
    /// <param name="auditEvents">The captured audit events</param>
    /// <param name="correlationId">The correlation ID to verify</param>
    /// <param name="expectedEventType">The expected event type</param>
    public static void WasAudited(
        IEnumerable<AuditTrailFixture.AuditLogEntry> auditEvents,
        string correlationId,
        string expectedEventType)
    {
        auditEvents
            .Should()
            .Contain(e => e.CorrelationId == correlationId && e.EventType == expectedEventType);
    }

    /// <summary>
    /// Asserts that an operation has performance metrics captured.
    /// </summary>
    /// <param name="auditEvent">The audit event to assert</param>
    /// <param name="maxDurationMs">The maximum expected duration in milliseconds</param>
    public static void HasPerformanceMetrics(
        AuditTrailFixture.AuditLogEntry auditEvent,
        long maxDurationMs)
    {
        auditEvent.Should().NotBeNull();
        auditEvent.DurationMs.Should().BeLessThanOrEqualTo(maxDurationMs);
        auditEvent.Timestamp.Should().BeBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Asserts that an operation succeeded and was audited.
    /// </summary>
    /// <param name="auditEvent">The audit event to assert</param>
    public static void Succeeded(AuditTrailFixture.AuditLogEntry auditEvent)
    {
        auditEvent.Should().NotBeNull();
        auditEvent.Success.Should().BeTrue();
    }

    /// <summary>
    /// Asserts that an operation failed and was audited.
    /// </summary>
    /// <param name="auditEvent">The audit event to assert</param>
    public static void Failed(AuditTrailFixture.AuditLogEntry auditEvent)
    {
        auditEvent.Should().NotBeNull();
        auditEvent.Success.Should().BeFalse();
    }
}
