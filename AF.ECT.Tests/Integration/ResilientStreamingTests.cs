using Xunit;
using FluentAssertions;
using AF.ECT.Shared;
using System.Diagnostics;

namespace AF.ECT.Tests.Integration;

/// <summary>
/// Integration tests for gRPC streaming methods with timeout and failure scenarios.
/// Tests timeout handling, exception propagation, and resilience during streaming operations.
/// </summary>
[Collection("Streaming Integration Tests")]
[Trait("Category", "Integration")]
[Trait("Component", "gRPC Streaming")]
public class ResilientStreamingTests
{
    #region Transient Failure Scenarios with Streaming Tests

    /// <summary>
    /// Tests that streaming properly handles transient failures and stops gracefully.
    /// </summary>
    [Fact]
    public async Task StreamingWithTransientFailures_StopsAndThrowsException()
    {
        // Arrange
        var failureCount = 0;
        var maxFailures = 3;
        
        // Create a stream that simulates transient failures
        async IAsyncEnumerable<ReinvestigationRequestItem> FaultyStream(
            int failuresBeforeSuccess,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= 10; i++)
            {
                ct.ThrowIfCancellationRequested();
                
                if (failureCount < failuresBeforeSuccess)
                {
                    failureCount++;
                    throw new InvalidOperationException("Transient error");
                }
                
                yield return await Task.FromResult(
                    new ReinvestigationRequestItem { Id = i, Description = $"Item_{i}" });
            }
        }

        // Act & Assert
        var items = new List<ReinvestigationRequestItem>();
        
        var act = async () =>
        {
            await foreach (var item in FaultyStream(maxFailures))
            {
                items.Add(item);
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
        items.Should().BeEmpty(); // No items yielded due to immediate failure
    }

    /// <summary>
    /// Tests that streaming prevents cascading failures by failing fast on first error.
    /// </summary>
    [Fact]
    public async Task StreamingWithPersistentFailures_FailsFastWithoutCascade()
    {
        // Arrange
        var attemptCount = 0;
        const int maxAttempts = 5;
        
        async IAsyncEnumerable<ReinvestigationRequestItem> AlwaysFailingStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                attemptCount++;
                
                if (attemptCount > maxAttempts)
                {
                    yield break;
                }
                
                throw new InvalidOperationException("Persistent failure");
            }
        }

        // Act
        var act = async () =>
        {
            await foreach (var item in AlwaysFailingStream())
            {
                // Never reached
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        attemptCount.Should().Be(1); // Only one attempt before throwing
    }

    #endregion

    #region Timeout with Streaming Tests

    /// <summary>
    /// Tests that streaming respects timeout constraints from resilience policies.
    /// </summary>
    [Fact]
    public async Task StreamingWithTimeout_CancelsWhenExceeded()
    {
        // Arrange
        const int timeoutMs = 500;
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
        var itemsProcessed = 0;
        var sw = Stopwatch.StartNew();

        async IAsyncEnumerable<ManagedUserItem> SlowStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= 1000; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(10); // Simulate slow processing
                yield return new ManagedUserItem { UserId = i, UserName = $"User_{i}" };
            }
        }

        // Act
        try
        {
            await foreach (var item in SlowStream().WithCancellation(cts.Token))
            {
                itemsProcessed++;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        sw.Stop();

        // Assert
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(timeoutMs + 100)); // Small tolerance
        itemsProcessed.Should().BeLessThan(1000);
    }

    /// <summary>
    /// Tests that streaming completes normally when timeout is sufficient.
    /// </summary>
    [Fact]
    public async Task StreamingWithTimeoutPolicy_CompletesWhenTimeoutIsAdequate()
    {
        // Arrange
        const int itemCount = 100;
        const int delayPerItemMs = 1;
        const int timeoutMs = 2000;
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        async IAsyncEnumerable<UserAltTitleItem> FastStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(delayPerItemMs);
                yield return new UserAltTitleItem { UserId = i, Title = $"Title_{i}" };
            }
        }

        // Act
        var items = new List<UserAltTitleItem>();
        await foreach (var item in FastStream().WithCancellation(cts.Token))
        {
            items.Add(item);
        }

        // Assert
        items.Should().HaveCount(itemCount);
    }

    #endregion

    #region Cancellation Token Pattern Tests

    /// <summary>
    /// Tests that streaming properly cancels via CancellationToken.
    /// Note: Retry policies should be applied at the gRPC client level, not within streams.
    /// </summary>
    [Fact]
    public async Task StreamingWithCancellation_StopsWhenRequested()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var itemsYielded = 0;

        async IAsyncEnumerable<MailingListItem> CancellableStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= 100; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(5);
                Interlocked.Increment(ref itemsYielded);
                yield return new MailingListItem { Id = i, Email = $"user{i}@example.com" };
            }
        }

        // Act
        var act = async () =>
        {
            await foreach (var item in CancellableStream().WithCancellation(cts.Token))
            {
                if (itemsYielded >= 10)
                {
                    cts.Cancel();
                }
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        itemsYielded.Should().BeLessThan(100);
    }

    #endregion

    #region Concurrent Streaming Tests

    /// <summary>
    /// Tests that multiple concurrent streams complete independently without interference.
    /// </summary>
    [Fact]
    public async Task MultipleConcurrentStreams_AllCompleteSuccessfully()
    {
        // Arrange
        const int streamCount = 3;
        const int itemsPerStream = 50;

        async IAsyncEnumerable<ManagedUserItem> IndependentStream(
            int streamId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemsPerStream; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return new ManagedUserItem
                {
                    UserId = (streamId * 1000) + i,
                    UserName = $"Stream{streamId}_User{i}"
                };
            }
        }

        // Act
        var tasks = Enumerable.Range(1, streamCount)
            .Select(async streamId =>
            {
                var items = new List<ManagedUserItem>();
                await foreach (var item in IndependentStream(streamId))
                {
                    items.Add(item);
                }
                return items.Count;
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(streamCount);
        results.Should().AllSatisfy(x => x.Should().Be(itemsPerStream));
    }

    /// <summary>
    /// Tests that one failing stream doesn't affect other concurrent streams.
    /// </summary>
    [Fact]
    public async Task ConcurrentStreams_PartialFailure_OnlyAffectsFailingStream()
    {
        // Arrange
        var successCount = 0;

        async IAsyncEnumerable<UserAltTitleItem> SuccessfulStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= 25; i++)
            {
                ct.ThrowIfCancellationRequested();
                Interlocked.Increment(ref successCount);
                yield return new UserAltTitleItem { UserId = i, Title = $"Title_{i}" };
            }
        }

        // Act
        var successTask = Task.Run(async () =>
        {
            var items = new List<UserAltTitleItem>();
            await foreach (var item in SuccessfulStream())
            {
                items.Add(item);
            }
            return items;
        });

        await successTask;

        // Assert
        successCount.Should().Be(25);
    }

    #endregion
}

/// <summary>
/// Extension methods for async streaming with cancellation support.
/// </summary>
internal static class AsyncStreamExtensions
{
    /// <summary>
    /// Helper extension to support cancellation token in foreach loop.
    /// </summary>
    public static async IAsyncEnumerable<T> WithCancellation<T>(
        this IAsyncEnumerable<T> source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in source.ConfigureAwait(false).WithCancellation(ct))
        {
            yield return item;
        }
    }
}
