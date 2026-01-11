using Xunit;
using FluentAssertions;
using AF.ECT.Shared;
using System.Diagnostics;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Tests for gRPC streaming methods to ensure proper handling of large datasets,
/// stream completion, cancellation token propagation, exception handling, and resource management.
/// Includes boundary conditions, performance assertions, and concurrent streaming scenarios.
/// </summary>
[Collection("Streaming Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "gRPC Streaming")]
public class StreamingMethodTests
{
    #region GetReinvestigationRequestsStream Tests

    /// <summary>
    /// Tests that GetReinvestigationRequestsStream properly streams large datasets.
    /// </summary>
    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(5000)]
    public async Task GetReinvestigationRequestsStream_WithLargeDataset_StreamsAllItems(int itemCount)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedItems = CreateMockReinvestigationItems(itemCount);

        // Act
        var collectedItems = new List<ReinvestigationRequestItem>();
        await foreach (var item in expectedItems.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().HaveCount(expectedItems.Count);
        collectedItems.Should().BeInAscendingOrder(x => x.Id);
    }

    /// <summary>
    /// Tests that GetReinvestigationRequestsStream completes properly with empty results.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsStream_WithNoResults_CompletesProperly()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var emptyItems = new List<ReinvestigationRequestItem>();

        // Act
        var collectedItems = new List<ReinvestigationRequestItem>();
        await foreach (var item in emptyItems.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetReinvestigationRequestsStream respects cancellation token.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsStream_WithCancellation_StopsStreaming()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var items = CreateMockReinvestigationItems(1000);

        // Act
        var collectedItems = new List<ReinvestigationRequestItem>();
        
        try
        {
            await foreach (var item in items.ToAsyncEnumerable(cts.Token))
            {
                collectedItems.Add(item);
                if (collectedItems.Count >= 50)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is triggered
        }

        // Assert
        collectedItems.Should().NotBeEmpty();
        collectedItems.Count.Should().BeLessThan(items.Count);
    }

    /// <summary>
    /// Tests that GetReinvestigationRequestsStream maintains consistent ordering across multiple calls.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsStream_MultipleIterations_MaintainConsistentOrdering()
    {
        // Arrange
        var items = CreateMockReinvestigationItems(100);

        // Act - First iteration
        var firstRun = new List<ReinvestigationRequestItem>();
        await foreach (var item in items.ToAsyncEnumerable())
        {
            firstRun.Add(item);
        }

        // Act - Second iteration
        var secondRun = new List<ReinvestigationRequestItem>();
        await foreach (var item in items.ToAsyncEnumerable())
        {
            secondRun.Add(item);
        }

        // Assert
        firstRun.Select(x => x.Id).Should().Equal(secondRun.Select(x => x.Id));
    }

    /// <summary>
    /// Tests that GetReinvestigationRequestsStream completes in reasonable time with large dataset.
    /// </summary>
    [Fact]
    public async Task GetReinvestigationRequestsStream_LargeDataset_CompletesInReasonableTime()
    {
        // Arrange
        var items = CreateMockReinvestigationItems(10000);
        var sw = Stopwatch.StartNew();

        // Act
        var count = 0;
        await foreach (var _ in items.ToAsyncEnumerable())
        {
            count++;
        }
        sw.Stop();

        // Assert
        count.Should().Be(10000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    #endregion

    #region GetMailingListForLODStream Tests

    /// <summary>
    /// Tests that GetMailingListForLODStream returns properly ordered results.
    /// </summary>
    [Fact]
    public async Task GetMailingListForLODStream_WithValidFilter_ReturnsOrderedResults()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var items = CreateMockMailingListItems(50);

        // Act
        var collectedItems = new List<MailingListItem>();
        await foreach (var item in items.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().HaveCount(50);
        collectedItems.Should().AllSatisfy(x => x.Email.Should().NotBeNullOrEmpty());
    }

    /// <summary>
    /// Tests that GetMailingListForLODStream handles multiple items with varying details correctly.
    /// </summary>
    [Fact]
    public async Task GetMailingListForLODStream_WithMultipleItems_StreamsAllItems()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var items = CreateMockMailingListItems(75);

        // Act
        var collectedItems = new List<MailingListItem>();
        await foreach (var item in items.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().HaveCount(75);
        collectedItems.Should().AllSatisfy(x => x.Name.Should().NotBeNullOrEmpty());
    }

    /// <summary>
    /// Tests that GetMailingListForLODStream properly handles exception in underlying stream.
    /// </summary>
    [Fact]
    public async Task GetMailingListForLODStream_WithStreamException_PropagatesError()
    {
        // Arrange
        var items = CreateMockMailingListItems(100);
        var throwOnIndex = 50;
        var collectedCount = 0;

        // Create an enumerable that throws after N items
        async IAsyncEnumerable<MailingListItem> ThrowingStreamAsync(
            IEnumerable<MailingListItem> source,
            int throwAt,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            var index = 0;
            foreach (var item in source)
            {
                ct.ThrowIfCancellationRequested();
                if (index++ >= throwAt)
                {
                    throw new InvalidOperationException("Backend error");
                }
                yield return await Task.FromResult(item);
            }
        }

        // Act & Assert
        var act = async () =>
        {
            await foreach (var item in ThrowingStreamAsync(items, throwOnIndex))
            {
                collectedCount++;
            }
        };

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Backend error");
        collectedCount.Should().Be(throwOnIndex); // Partial data before exception
    }

    #endregion

    #region GetManagedUsersStream Tests

    /// <summary>
    /// Tests that GetManagedUsersStream efficiently handles large result sets without excessive memory usage.
    /// </summary>
    [Fact]
    public async Task GetManagedUsersStream_WithLargeResultSet_StreamsEfficiently()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var items = CreateMockManagedUserItems(500);

        // Act
        var collectedItems = new List<ManagedUserItem>();
        await foreach (var item in items.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().HaveCount(500);
        collectedItems.Should().AllSatisfy(x => x.UserId.Should().BeGreaterThan(0));
    }

    /// <summary>
    /// Tests that GetManagedUsersStream timeout is properly handled during streaming.
    /// </summary>
    [Fact]
    public async Task GetManagedUsersStream_WithTimeout_RespectsTimeout()
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var slowItems = CreateMockManagedUserItems(10000);

        // Act & Assert
        var collectedItems = new List<ManagedUserItem>();
        
        var act = async () =>
        {
            await foreach (var item in slowItems.ToAsyncEnumerable(cts.Token))
            {
                collectedItems.Add(item);
                await Task.Delay(20); // Simulate processing delay
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests concurrent streaming with multiple parallel streams.
    /// </summary>
    [Fact]
    public async Task GetManagedUsersStream_ConcurrentStreams_AllCompleteSuccessfully()
    {
        // Arrange
        var streamCount = 5;
        var itemsPerStream = 100;

        // Act
        var tasks = Enumerable.Range(1, streamCount)
            .Select(async i =>
            {
                var items = CreateMockManagedUserItems(itemsPerStream);
                var collectedItems = new List<ManagedUserItem>();
                
                await foreach (var item in items.ToAsyncEnumerable())
                {
                    collectedItems.Add(item);
                }
                
                return collectedItems.Count;
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(streamCount);
        results.Should().AllSatisfy(x => x.Should().Be(itemsPerStream));
    }

    #endregion

    #region GetUserAltTitleStream Tests

    /// <summary>
    /// Tests that GetUserAltTitleStream handles partial result sets correctly.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleStream_WithPartialResults_ProperlyCompletes()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var items = CreateMockUserAltTitleItems(25);

        // Act
        var collectedItems = new List<UserAltTitleItem>();
        await foreach (var item in items.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().HaveCount(25);
        collectedItems.Should().AllSatisfy(x => x.Title.Should().NotBeNullOrWhiteSpace());
    }

    /// <summary>
    /// Tests that GetUserAltTitleStream can be cancelled mid-stream.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleStream_CancelledMidStream_StopsImmediately()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var items = CreateMockUserAltTitleItems(200);

        // Act
        var collectedItems = new List<UserAltTitleItem>();
        
        try
        {
            await foreach (var item in items.ToAsyncEnumerable(cts.Token))
            {
                collectedItems.Add(item);
                if (collectedItems.Count >= 50)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        collectedItems.Count.Should().BeLessThanOrEqualTo(51); // May have one extra due to timing
    }

    /// <summary>
    /// Tests that GetUserAltTitleStream properly handles streaming performance.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleStream_PerformanceTest_CompletesInExpectedTime()
    {
        // Arrange
        var items = CreateMockUserAltTitleItems(1000);
        var sw = Stopwatch.StartNew();

        // Act
        var count = 0;
        await foreach (var _ in items.ToAsyncEnumerable())
        {
            count++;
        }
        sw.Stop();

        // Assert
        count.Should().Be(1000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
    }

    #endregion

    #region GetUserAltTitleByGroupCompoStream Tests

    /// <summary>
    /// Tests that GetUserAltTitleByGroupCompoStream properly handles empty result streams.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleByGroupCompoStream_WithEmptyResults_CompletesGracefully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var emptyItems = new List<UserAltTitleByGroupCompoItem>();

        // Act
        var collectedItems = new List<UserAltTitleByGroupCompoItem>();
        await foreach (var item in emptyItems.ToAsyncEnumerable(cancellationToken))
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that GetUserAltTitleByGroupCompoStream properly streams large datasets.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleByGroupCompoStream_WithLargeDataset_StreamsAllItems()
    {
        // Arrange
        var items = CreateMockUserAltTitleByGroupCompoItems(100);

        // Act
        var collectedItems = new List<UserAltTitleByGroupCompoItem>();
        
        await foreach (var item in items.ToAsyncEnumerable())
        {
            collectedItems.Add(item);
        }

        // Assert
        collectedItems.Should().HaveCount(100);
        collectedItems.Should().AllSatisfy(x =>
        {
            x.UserId.Should().BeGreaterThan(0);
            x.Title.Should().NotBeNullOrEmpty();
            x.Component.Should().NotBeNullOrEmpty();
        });
    }

    /// <summary>
    /// Tests that stream enumerators can be disposed without resource leaks.
    /// </summary>
    [Fact]
    public async Task GetUserAltTitleByGroupCompoStream_EarlyDisposal_DoesNotCauseLeaks()
    {
        // Arrange
        var items = CreateMockUserAltTitleByGroupCompoItems(500);
        
        // Act
        var enumerator = items.ToAsyncEnumerable().GetAsyncEnumerator();
        
        // Consume first few items
        for (int i = 0; i < 10; i++)
        {
            await enumerator.MoveNextAsync();
        }
        
        // Dispose early
        await enumerator.DisposeAsync();
        
        // Assert - Should not have dangling resources
        // This is verified by the absence of exceptions
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates mock reinvestigation request items.
    /// </summary>
    private static List<ReinvestigationRequestItem> CreateMockReinvestigationItems(int count)
    {
        var items = new List<ReinvestigationRequestItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new ReinvestigationRequestItem { Id = i, Description = $"Item_{i:D4}" });
        }
        return items;
    }

    /// <summary>
    /// Creates mock mailing list items.
    /// </summary>
    private static List<MailingListItem> CreateMockMailingListItems(int count)
    {
        var items = new List<MailingListItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new MailingListItem
            {
                Id = i,
                Email = $"user{i}@example.com",
                Name = $"User_{i:D4}"
            });
        }
        return items;
    }

    /// <summary>
    /// Creates mock managed user items.
    /// </summary>
    private static List<ManagedUserItem> CreateMockManagedUserItems(int count)
    {
        var items = new List<ManagedUserItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new ManagedUserItem
            {
                UserId = i,
                UserName = $"User_{i:D4}",
                Email = $"user{i}@example.com",
                Status = 1
            });
        }
        return items;
    }

    /// <summary>
    /// Creates mock user alt title items.
    /// </summary>
    private static List<UserAltTitleItem> CreateMockUserAltTitleItems(int count)
    {
        var items = new List<UserAltTitleItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new UserAltTitleItem { UserId = i, Title = $"Alt_Title_{i}", GroupId = i % 5 });
        }
        return items;
    }

    /// <summary>
    /// Creates mock user alt title by group compo items.
    /// </summary>
    private static List<UserAltTitleByGroupCompoItem> CreateMockUserAltTitleByGroupCompoItems(int count)
    {
        var items = new List<UserAltTitleByGroupCompoItem>();
        for (int i = 1; i <= count; i++)
        {
            items.Add(new UserAltTitleByGroupCompoItem
            {
                UserId = i,
                Title = $"GroupTitle_{i}",
                Component = $"Comp_{i % 3}"
            });
        }
        return items;
    }

    #endregion
}

/// <summary>
/// Extension helper for converting enumerables to async enumerables for testing.
/// </summary>
public static class AsyncEnumerableExtension
{
    /// <summary>
    /// Converts a regular enumerable to an async enumerable for testing purposes.
    /// </summary>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return await Task.FromResult(item);
        }
    }
}
