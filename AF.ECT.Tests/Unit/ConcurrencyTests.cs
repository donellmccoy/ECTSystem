namespace AF.ECT.Tests.Unit;

using FluentAssertions;
using System.Collections.Concurrent;

/// <summary>
/// Contains concurrency tests for race condition detection, deadlock prevention, and lock contention analysis.
/// Tests multi-threaded scenarios and concurrent operations.
/// </summary>
[Collection("Concurrency Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "Concurrency")]
public class ConcurrencyTests
{
    /// <summary>
    /// Tests that concurrent list access doesn't cause race conditions with thread-safe collections.
    /// </summary>
    [Fact]
    public async Task ConcurrentListAccess_WithConcurrentBag_IsThreadSafe()
    {
        // Arrange
        var bag = new ConcurrentBag<int>();
        var tasks = new List<Task>();
        var itemCount = 1000;

        // Act
        for (int i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < itemCount; j++)
                {
                    bag.Add(threadId * itemCount + j);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        bag.Should().HaveCount(10 * itemCount, "All items should be added without loss");
    }

    /// <summary>
    /// Tests that concurrent dictionary updates don't cause data corruption.
    /// </summary>
    [Fact]
    public async Task ConcurrentDictionaryUpdates_AreConsistent()
    {
        // Arrange
        var dict = new ConcurrentDictionary<int, int>();
        var tasks = new List<Task>();
        var iterations = 100;

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterations; j++)
                {
                    dict.AddOrUpdate(j, 1, (key, oldVal) => oldVal + 1);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        dict.Should().HaveCount(iterations, "All keys should be present");
        dict.Values.All(v => v == 10).Should().BeTrue("Each key should be updated 10 times");
    }

    /// <summary>
    /// Tests that readonly lock prevents concurrent modifications.
    /// </summary>
    [Fact]
    public async Task ReadWriteLock_PreventsRaceConditions()
    {
        // Arrange
        var readerWriterLock = new ReaderWriterLockSlim();
        var value = 0;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    readerWriterLock.EnterWriteLock();
                    try
                    {
                        value++;
                    }
                    finally
                    {
                        readerWriterLock.ExitWriteLock();
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        value.Should().Be(500, "All increments should be executed without loss");
        readerWriterLock.Dispose();
    }

    /// <summary>
    /// Tests that semaphore prevents resource exhaustion from concurrent access.
    /// </summary>
    [Fact]
    public async Task Semaphore_LimitsMaximumConcurrency()
    {
        // Arrange
        var semaphore = new SemaphoreSlim(3); // Max 3 concurrent
        var concurrentCount = 0;
        var maxConcurrentObserved = 0;
        var lockObj = new object();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    lock (lockObj)
                    {
                        concurrentCount++;
                        maxConcurrentObserved = Math.Max(maxConcurrentObserved, concurrentCount);
                    }

                    await Task.Delay(10);

                    lock (lockObj)
                    {
                        concurrentCount--;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        maxConcurrentObserved.Should().BeLessThanOrEqualTo(3,
            "Semaphore should limit concurrent access to 3");
    }

    /// <summary>
    /// Tests that mutex prevents simultaneous access.
    /// </summary>
    [Fact]
    public async Task Mutex_PreventsSimultaneousAccess()
    {
        // Arrange
        var mutex = new Mutex();
        var sharedValue = 0;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    mutex.WaitOne();
                    try
                    {
                        sharedValue++;
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        sharedValue.Should().Be(500, "All operations should complete without loss");
        mutex.Dispose();
    }

    /// <summary>
    /// Tests that task cancellation is properly propagated.
    /// </summary>
    [Fact]
    public async Task CancellationToken_StopsAllConcurrentTasks()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var taskCount = 0;
        var completedCount = 0;
        var lockObj = new object();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            lock (lockObj)
            {
                taskCount++;
            }

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(10);
                    }

                    lock (lockObj)
                    {
                        completedCount++;
                    }
                }
                catch (OperationCanceledException)
                {
                    lock (lockObj)
                    {
                        completedCount++;
                    }
                }
            }));
        }

        await Task.Delay(50);
        cts.Cancel();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        completedCount.Should().Be(taskCount, "All tasks should complete after cancellation");
    }

    /// <summary>
    /// Tests that async lock prevents concurrent modification.
    /// </summary>
    [Fact]
    public async Task AsyncLock_PreventsRaceConditions()
    {
        // Arrange
        var asyncLock = new SemaphoreSlim(1, 1);
        var value = 0;
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 100; j++)
                {
                    await asyncLock.WaitAsync();
                    try
                    {
                        value++;
                        await Task.Yield(); // Simulate async work
                    }
                    finally
                    {
                        asyncLock.Release();
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        value.Should().Be(500, "All increments should execute atomically");
    }

    /// <summary>
    /// Tests that thread pool queuing doesn't cause deadlock.
    /// </summary>
    [Fact]
    public void ThreadPoolQueuing_DoesNotCauseDeadlock()
    {
        // Arrange
        var manualResetEvent = new ManualResetEvent(false);
        var taskCompleted = false;

        // Act
        ThreadPool.QueueUserWorkItem(state =>
        {
            Task.Delay(10).Wait();
            taskCompleted = true;
            manualResetEvent.Set();
        });

        var completed = manualResetEvent.WaitOne(TimeSpan.FromSeconds(5));

        // Assert
        completed.Should().BeTrue("Task should complete without deadlock");
        taskCompleted.Should().BeTrue();
    }

    /// <summary>
    /// Tests that async/await properly handles task continuations.
    /// </summary>
    [Fact]
    public async Task AsyncAwait_ProperlyHandlesContinuations()
    {
        // Arrange
        var results = new ConcurrentBag<int>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(10);
                results.Add(taskId);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(10, "All continuations should execute");
        results.Distinct().Count().Should().Be(10, "All tasks should run with unique IDs");
    }

    /// <summary>
    /// Tests that lock contention is acceptable under load.
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task LockContention_DoesNotCauseTimeout()
    {
        // Arrange
        var lockObj = new object();
        var counter = 0;
        var tasks = new List<Task>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 1000; j++)
                {
                    lock (lockObj)
                    {
                        counter++;
                    }
                }
            }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        counter.Should().Be(10000, "All iterations should complete");
        sw.ElapsedMilliseconds.Should().BeLessThan(5000, "Should complete within 5 seconds");
    }
}
