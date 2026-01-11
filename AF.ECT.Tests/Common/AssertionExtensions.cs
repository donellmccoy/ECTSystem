using System.Linq.Expressions;

namespace AF.ECT.Tests.Common;

/// <summary>
/// Custom assertion extensions for ECTSystem-specific testing scenarios.
/// Provides fluent assertions for gRPC responses, audit logs, and domain-specific validations.
/// </summary>
public static class AssertionExtensions
{
    /// <summary>
    /// Asserts that a gRPC response contains expected items and has valid metadata.
    /// </summary>
    /// <typeparam name="T">The item type in the response collection</typeparam>
    /// <param name="items">The response items collection</param>
    /// <param name="expectedCount">Expected number of items</param>
    /// <returns>The items collection for chaining</returns>
    public static IEnumerable<T> ShouldHaveValidGrpcResponse<T>(
        this IEnumerable<T> items,
        int expectedCount)
        where T : class
    {
        items.Should().NotBeNull("gRPC response items should not be null");
        items.Should().HaveCount(expectedCount, $"expected {expectedCount} items in gRPC response");
        return items;
    }

    /// <summary>
    /// Asserts that a gRPC RpcException has the expected status code.
    /// </summary>
    /// <param name="exception">The RpcException to validate</param>
    /// <param name="expectedStatus">The expected status code</param>
    /// <returns>The exception for chaining</returns>
    public static RpcException ShouldHaveGrpcStatus(
        this RpcException exception,
        StatusCode expectedStatus)
    {
        exception.Should().NotBeNull("RpcException should not be null");
        exception.StatusCode.Should().Be(expectedStatus, 
            $"gRPC status should be {expectedStatus}, but was {exception.StatusCode}");
        return exception;
    }

    /// <summary>
    /// Asserts that a result returned from a data service call is not null and valid.
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="result">The data service result</param>
    /// <returns>The result for chaining</returns>
    public static T ShouldBeValidDataServiceResult<T>(this T result)
        where T : class
    {
        result.Should().NotBeNull("data service result should not be null");
        return result;
    }

    /// <summary>
    /// Asserts that a collection of data service results is non-empty.
    /// </summary>
    /// <typeparam name="T">The item type</typeparam>
    /// <param name="results">The collection to validate</param>
    /// <param name="minimumCount">Minimum expected item count</param>
    /// <returns>The results collection for chaining</returns>
    public static IList<T> ShouldHaveMinimumResults<T>(
        this IList<T> results,
        int minimumCount = 1)
        where T : class
    {
        results.Should().NotBeNull("results collection should not be null");
        results.Should().HaveCountGreaterThanOrEqualTo(minimumCount,
            $"expected at least {minimumCount} results");
        return results;
    }

    /// <summary>
    /// Asserts that an async task completes within a reasonable timeout.
    /// Useful for verifying streaming operations don't hang.
    /// </summary>
    /// <param name="task">The task to validate</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <returns>A completed task</returns>
    public static async Task ShouldCompleteWithinTimeoutAsync(
        this Task task,
        int timeoutMs = 5000)
    {
        task.Should().NotBeNull("task should not be null");
        
        var completedTask = await Task.WhenAny(
            task,
            Task.Delay(timeoutMs));

        completedTask.Should().Be(task,
            $"task should complete within {timeoutMs}ms");
    }

    /// <summary>
    /// Asserts that an async task completes within a reasonable timeout and returns expected value.
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="task">The task returning T to validate</param>
    /// <param name="timeoutMs">Timeout in milliseconds (default: 5000)</param>
    /// <returns>The task result for chaining</returns>
    public static async Task<T> ShouldCompleteWithinTimeoutAsync<T>(
        this Task<T> task,
        int timeoutMs = 5000)
    {
        task.Should().NotBeNull("task should not be null");
        
        var completedTask = await Task.WhenAny(
            task,
            Task.Delay(timeoutMs));

        completedTask.Should().Be(task,
            $"task should complete within {timeoutMs}ms");

        return await task;
    }

    /// <summary>
    /// Asserts that a method was called with specific parameters, with detailed message on failure.
    /// </summary>
    /// <typeparam name="T">The mock type</typeparam>
    /// <param name="mock">The mock to verify</param>
    /// <param name="verificationAction">The verification action (e.g., m => m.Method(...))</param>
    /// <param name="times">Expected call count</param>
    /// <returns>The mock for chaining</returns>
    public static Mock<T> ShouldHaveBeenCalledWith<T>(
        this Mock<T> mock,
        Expression<Action<T>> verificationAction,
        Times? times = null)
        where T : class
    {
        mock.Should().NotBeNull("mock should not be null");
        
        if (times.HasValue)
        {
            mock.Verify(verificationAction, times.Value);
        }
        else
        {
            mock.Verify(verificationAction, Times.Once);
        }

        return mock;
    }
}
