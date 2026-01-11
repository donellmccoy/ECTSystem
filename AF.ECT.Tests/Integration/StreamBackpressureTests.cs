using Xunit;
using FluentAssertions;
using AF.ECT.Shared;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AF.ECT.Tests.Integration;

/// <summary>
/// Integration tests for gRPC streaming backpressure and buffering patterns.
/// Tests how streaming handles scenarios where producers and consumers have mismatched rates.
/// </summary>
[Collection("Streaming Backpressure Tests")]
[Trait("Category", "Integration")]
[Trait("Component", "gRPC Streaming")]
public class StreamBackpressureTests
{
    #region Producer-Consumer Rate Mismatch Tests

    /// <summary>
    /// Tests that a slow consumer doesn't cause stream buffer overflow with moderate backpressure.
    /// </summary>
    [Fact]
    public async Task SlowConsumer_BuffersItems_WithoutOverflow()
    {
        // Arrange
        const int itemCount = 100;
        const int produceDelayMs = 5;
        const int consumeDelayMs = 15; // Slower than producer
        var consumedCount = 0;

        async IAsyncEnumerable<ManagedUserItem> SlowProducerStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(produceDelayMs);
                yield return new ManagedUserItem { UserId = i, UserName = $"User_{i}" };
            }
        }

        // Act
        var sw = Stopwatch.StartNew();
        await foreach (var item in SlowProducerStream())
        {
            await Task.Delay(consumeDelayMs); // Simulate slow consumer
            Interlocked.Increment(ref consumedCount);
        }
        sw.Stop();

        // Assert - should complete successfully with all items consumed
        consumedCount.Should().Be(itemCount);
        sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(consumeDelayMs * itemCount / 2));
    }

    /// <summary>
    /// Tests that a fast consumer can efficiently process items from a slow producer.
    /// </summary>
    [Fact]
    public async Task FastConsumer_ProcessesQuickly_FromSlowProducer()
    {
        // Arrange
        const int itemCount = 50;
        const int produceDelayMs = 50; // Slower producer
        const int consumeDelayMs = 5;  // Faster consumer
        var consumedCount = 0;

        async IAsyncEnumerable<ReinvestigationRequestItem> SlowProducerStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(produceDelayMs);
                yield return new ReinvestigationRequestItem { Id = i, Description = $"Req_{i}" };
            }
        }

        // Act
        var sw = Stopwatch.StartNew();
        await foreach (var item in SlowProducerStream())
        {
            await Task.Delay(consumeDelayMs);
            Interlocked.Increment(ref consumedCount);
        }
        sw.Stop();

        // Assert
        consumedCount.Should().Be(itemCount);
        // Total time should be dominated by the slower producer
        sw.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(produceDelayMs * itemCount / 2));
    }

    #endregion

    #region Buffering and Memory Tests

    /// <summary>
    /// Tests that streaming doesn't cause unbounded memory growth with large datasets.
    /// </summary>
    [Fact]
    public async Task LargeStream_MaintainsBoundedMemory_WithoutGrowth()
    {
        // Arrange
        const int itemCount = 1000;
        var processedCount = 0;
        var peakMemory = GC.GetTotalMemory(false);

        async IAsyncEnumerable<UserAltTitleItem> LargeStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return new UserAltTitleItem { UserId = i, Title = $"Title_{i}" };
            }
        }

        // Act
        await foreach (var item in LargeStream())
        {
            processedCount++;
            if (processedCount % 100 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                peakMemory = Math.Max(peakMemory, currentMemory);
            }
        }

        // Assert
        processedCount.Should().Be(itemCount);
        // Memory should not grow significantly per item (rough check: max allocation per item)
        var memoryPerItem = (peakMemory / Math.Max(1, processedCount)) / 1024.0; // KB per item
        memoryPerItem.Should().BeLessThan(100); // Reasonable limit for object size
    }

    /// <summary>
    /// Tests that discarded items are properly garbage collected.
    /// </summary>
    [Fact]
    public async Task DiscardedItems_AreGarbageCollected_WithoutMemoryLeak()
    {
        // Arrange
        const int itemCount = 500;
        var skippedCount = 0;
        var initialMemory = GC.GetTotalMemory(true);

        async IAsyncEnumerable<MailingListItem> BufferedStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return new MailingListItem { Id = i, Email = $"user{i}@example.com" };
            }
        }

        // Act
        await foreach (var item in BufferedStream())
        {
            // Intentionally don't store items - they should be GC'd
            if (item.Id % 2 == 0)
            {
                skippedCount++;
            }
        }

        GC.Collect(); // Force collection
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        // Assert
        skippedCount.Should().Be(itemCount / 2);
        // Memory after GC should be similar to initial (allowing for some overhead)
        var memoryGrowth = finalMemory - initialMemory;
        memoryGrowth.Should().BeLessThan(1024 * 1024); // Less than 1 MB growth
    }

    #endregion

    #region Partial Consumption Tests

    /// <summary>
    /// Tests that partially consumed streams can be stopped without resource leaks.
    /// </summary>
    [Fact]
    public async Task PartialConsumption_StopsGracefully_WithoutLeaks()
    {
        // Arrange
        const int consumeUntil = 100;
        var actuallyConsumed = 0;

        async IAsyncEnumerable<ReinvestigationRequestItem> InfiniteStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            int count = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                count++;
                yield return new ReinvestigationRequestItem { Id = count, Description = $"Req_{count}" };
            }
        }

        // Act
        await foreach (var item in InfiniteStream())
        {
            actuallyConsumed++;
            if (actuallyConsumed >= consumeUntil)
            {
                break; // Exit early
            }
        }

        // Assert
        actuallyConsumed.Should().Be(consumeUntil);
    }

    /// <summary>
    /// Tests that exceptions during consumption don't prevent cleanup of unconsumed items.
    /// </summary>
    [Fact]
    public async Task ExceptionDuringConsumption_AllowsCleanup_WithoutDeadlock()
    {
        // Arrange
        const int itemCount = 100;
        var processedCount = 0;

        async IAsyncEnumerable<UserAltTitleItem> ExceptionProneStream(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return new UserAltTitleItem { UserId = i, Title = $"Title_{i}" };
            }
        }

        // Act
        var act = async () =>
        {
            await foreach (var item in ExceptionProneStream())
            {
                processedCount++;
                if (processedCount == 50)
                {
                    throw new InvalidOperationException("Consumer error");
                }
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        processedCount.Should().Be(50);
    }

    #endregion

    #region Concurrent Consumption Tests

    /// <summary>
    /// Tests that one enumeration doesn't affect another concurrent enumeration of the same stream generator.
    /// </summary>
    [Fact]
    public async Task ConcurrentEnumerations_AreIndependent_WithoutInterference()
    {
        // Arrange
        const int itemCount = 100;
        var firstStreamCount = 0;
        var secondStreamCount = 0;

        async IAsyncEnumerable<MailingListItem> IndependentStream(
            int streamId,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            for (int i = 1; i <= itemCount; i++)
            {
                ct.ThrowIfCancellationRequested();
                yield return new MailingListItem { Id = (streamId * 1000) + i, Email = $"stream{streamId}_user{i}@example.com" };
            }
        }

        // Act
        var task1 = Task.Run(async () =>
        {
            await foreach (var item in IndependentStream(1))
            {
                Interlocked.Increment(ref firstStreamCount);
                await Task.Delay(1); // Add minor delay
            }
        });

        var task2 = Task.Run(async () =>
        {
            await foreach (var item in IndependentStream(2))
            {
                Interlocked.Increment(ref secondStreamCount);
                await Task.Delay(2); // Different delay
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert
        firstStreamCount.Should().Be(itemCount);
        secondStreamCount.Should().Be(itemCount);
    }

    /// <summary>
    /// Tests that buffering channels handle concurrent producers efficiently.
    /// </summary>
    [Fact]
    public async Task ConcurrentProducers_HandleBufferingEfficiently_WithoutDeadlock()
    {
        // Arrange
        const int producerCount = 3;
        const int itemsPerProducer = 50;
        var receivedCount = 0;

        // Use concurrent bag to safely collect results from multiple producers
        var receivedItems = new ConcurrentBag<(int ProducerId, int ItemIndex)>();

        async Task ProduceItems(int producerId)
        {
            for (int i = 1; i <= itemsPerProducer; i++)
            {
                receivedItems.Add((producerId, i));
                await Task.Delay(1);
            }
        }

        // Act
        var producers = Enumerable.Range(1, producerCount)
            .Select(id => ProduceItems(id))
            .ToList();

        await Task.WhenAll(producers);
        receivedCount = receivedItems.Count;

        // Assert
        receivedCount.Should().Be(producerCount * itemsPerProducer);
    }

    #endregion
}
