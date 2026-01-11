using FluentAssertions;
using FluentAssertions.Primitives;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Custom fluent assertion extensions for gRPC streaming tests.
/// Provides convenient methods for asserting common streaming patterns and behaviors.
/// </summary>
public static class StreamTestAssertions
{
    /// <summary>
    /// Asserts that a stream completes successfully and contains the expected number of items.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="stream">The async enumerable stream.</param>
    /// <param name="expectedCount">The expected number of items.</param>
    /// <returns>The collected items for further assertions.</returns>
    public static async Task<List<T>> ShouldStreamSuccessfully<T>(
        this IAsyncEnumerable<T> stream, 
        int expectedCount)
    {
        var items = new List<T>();
        
        await foreach (var item in stream)
        {
            items.Add(item);
        }
        
        items.Should().HaveCount(expectedCount);
        return items;
    }

    /// <summary>
    /// Asserts that a stream is properly ordered by a given key selector.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <typeparam name="TKey">The type of the ordering key.</typeparam>
    /// <param name="stream">The async enumerable stream.</param>
    /// <param name="keySelector">The function to extract the comparison key.</param>
    /// <param name="ascending">Whether to assert ascending (true) or descending (false) order.</param>
    /// <returns>The collected items for further assertions.</returns>
    public static async Task<List<T>> ShouldBeOrderedBy<T, TKey>(
        this IAsyncEnumerable<T> stream, 
        Func<T, TKey> keySelector, 
        bool ascending = true) where TKey : IComparable<TKey>
    {
        var items = new List<T>();
        
        await foreach (var item in stream)
        {
            items.Add(item);
        }
        
        // Verify ordering by comparing consecutive elements
        for (int i = 0; i < items.Count - 1; i++)
        {
            var currentKey = keySelector(items[i]);
            var nextKey = keySelector(items[i + 1]);
            var comparison = currentKey.CompareTo(nextKey);
            
            if (ascending)
            {
                comparison.Should().BeLessThanOrEqualTo(0, "Items should be in ascending order");
            }
            else
            {
                comparison.Should().BeGreaterThanOrEqualTo(0, "Items should be in descending order");
            }
        }
        
        return items;
    }

    /// <summary>
    /// Asserts that a stream completes successfully even though it's empty.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="stream">The async enumerable stream.</param>
    public static async Task ShouldCompleteEmpty<T>(this IAsyncEnumerable<T> stream)
    {
        var items = new List<T>();
        
        await foreach (var item in stream)
        {
            items.Add(item);
        }
        
        items.Should().BeEmpty();
    }

    /// <summary>
    /// Asserts that a stream can be cancelled gracefully after consuming N items.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="stream">The async enumerable stream.</param>
    /// <param name="itemsBeforeCancel">The number of items to consume before cancellation.</param>
    /// <returns>The number of items collected before cancellation.</returns>
    public static async Task<int> ShouldBeCancellableAfter<T>(
        this IAsyncEnumerable<T> stream, 
        int itemsBeforeCancel)
    {
        var cts = new CancellationTokenSource();
        var collectedCount = 0;
        
        try
        {
            await foreach (var item in stream.WithCancellation(cts.Token))
            {
                collectedCount++;
                if (collectedCount >= itemsBeforeCancel)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
        
        collectedCount.Should().BeLessThanOrEqualTo(itemsBeforeCancel + 1);
        return collectedCount;
    }

    /// <summary>
    /// Asserts that a stream contains items satisfying a given predicate.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="stream">The async enumerable stream.</param>
    /// <param name="predicate">The condition to check.</param>
    /// <param name="expectedCount">The expected number of matching items (-1 for any).</param>
    /// <returns>The collected items for further assertions.</returns>
    public static async Task<List<T>> ShouldContainItemsMatching<T>(
        this IAsyncEnumerable<T> stream, 
        Func<T, bool> predicate, 
        int expectedCount = -1)
    {
        var items = new List<T>();
        var matchingItems = new List<T>();
        
        await foreach (var item in stream)
        {
            items.Add(item);
            if (predicate(item))
            {
                matchingItems.Add(item);
            }
        }
        
        if (expectedCount >= 0)
        {
            matchingItems.Should().HaveCount(expectedCount);
        }
        else
        {
            matchingItems.Should().NotBeEmpty();
        }
        
        return items;
    }

    /// <summary>
    /// Asserts that a stream respects a timeout constraint.
    /// </summary>
    /// <typeparam name="T">The type of items in the stream.</typeparam>
    /// <param name="stream">The async enumerable stream.</param>
    /// <param name="timeout">The maximum time allowed to complete the stream.</param>
    /// <returns>The collected items for further assertions.</returns>
    public static async Task<List<T>> ShouldCompleteWithin<T>(
        this IAsyncEnumerable<T> stream, 
        TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        var items = new List<T>();
        
        await foreach (var item in stream.WithCancellation(cts.Token))
        {
            items.Add(item);
        }
        
        return items;
    }

    /// <summary>
    /// Asserts that multiple parallel streams complete successfully.
    /// </summary>
    /// <typeparam name="T">The type of items in the streams.</typeparam>
    /// <param name="streams">The collection of async enumerable streams.</param>
    /// <param name="itemsPerStream">The expected number of items per stream.</param>
    public static async Task AllShouldStreamSuccessfully<T>(
        this IEnumerable<IAsyncEnumerable<T>> streams, 
        int itemsPerStream)
    {
        var tasks = streams.Select(async stream =>
        {
            var items = await stream.ShouldStreamSuccessfully(itemsPerStream);
            return items.Count;
        }).ToList();
        
        var results = await Task.WhenAll(tasks);
        results.Should().AllSatisfy(x => x.Should().Be(itemsPerStream));
    }

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
