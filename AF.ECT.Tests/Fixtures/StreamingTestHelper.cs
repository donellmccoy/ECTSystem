namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Helper for testing gRPC streaming operations, timeout scenarios, and error injection.
/// Provides utilities for validating streaming behavior, cancellation, and error handling.
/// </summary>
public static class StreamingTestHelper
{
    /// <summary>
    /// Represents a configuration for a test streaming scenario.
    /// </summary>
    public class StreamConfiguration
    {
        /// <summary>
        /// Gets the total number of items to stream.
        /// </summary>
        public int TotalItemCount { get; init; }

        /// <summary>
        /// Gets the delay between items (in milliseconds).
        /// </summary>
        public int DelayBetweenItemsMs { get; init; }

        /// <summary>
        /// Gets the timeout for the entire stream (in milliseconds).
        /// </summary>
        public int TimeoutMs { get; init; }

        /// <summary>
        /// Gets the item number after which to inject an error (null for no error).
        /// </summary>
        public int? ErrorAfterItemNumber { get; init; }

        /// <summary>
        /// Gets the error to inject into the stream.
        /// </summary>
        public Exception? ErrorToInject { get; init; }
    }

    /// <summary>
    /// Represents the result of streaming validation.
    /// </summary>
    public class StreamResult
    {
        /// <summary>
        /// Gets the items successfully received before completion or error.
        /// </summary>
        public List<object> ItemsReceived { get; } = [];

        /// <summary>
        /// Gets the exception that occurred during streaming (null if completed successfully).
        /// </summary>
        public Exception? StreamException { get; set; }

        /// <summary>
        /// Gets the total time elapsed during streaming.
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// Gets whether the stream completed successfully.
        /// </summary>
        public bool CompletedSuccessfully => StreamException is null;

        /// <summary>
        /// Gets whether the stream timed out.
        /// </summary>
        public bool TimedOut => StreamException is OperationCanceledException;

        /// <summary>
        /// Gets a summary of the streaming result for assertions.
        /// </summary>
        public string GetSummary() => 
            $"Items: {ItemsReceived.Count}, Completed: {CompletedSuccessfully}, TimedOut: {TimedOut}, Duration: {ElapsedTime.TotalMilliseconds}ms";
    }

    /// <summary>
    /// Simulates a streaming server response with configurable items, delays, and error injection.
    /// </summary>
    public static async IAsyncEnumerable<T> CreateSimulatedStream<T>(
        StreamConfiguration config,
        Func<int, T> itemFactory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= config.TotalItemCount; i++)
        {
            // Check for cancellation before yielding each item
            cancellationToken.ThrowIfCancellationRequested();

            // Inject error at specified item number
            if (config.ErrorAfterItemNumber.HasValue && i == config.ErrorAfterItemNumber)
            {
                if (config.ErrorToInject != null)
                    throw config.ErrorToInject;
                throw new InvalidOperationException($"Simulated error after item {i}");
            }

            yield return itemFactory(i);

            // Add delay between items (but check cancellation first)
            if (i < config.TotalItemCount && config.DelayBetweenItemsMs > 0)
            {
                await Task.Delay(config.DelayBetweenItemsMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Consumes a stream with timeout and error handling.
    /// </summary>
    public static async Task<StreamResult> ConsumeStreamWithTimeout<T>(
        IAsyncEnumerable<T> stream,
        int timeoutMs = 5000)
    {
        var result = new StreamResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));

        try
        {
            await foreach (var item in stream.WithCancellation(cts.Token))
            {
                result.ItemsReceived.Add(item ?? new object());

                // Additional timeout check every N items
                if (result.ItemsReceived.Count % 10 == 0 && stopwatch.ElapsedMilliseconds > timeoutMs)
                {
                    throw new OperationCanceledException("Stream exceeded timeout");
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            result.StreamException = ex;
        }
        catch (Exception ex)
        {
            result.StreamException = ex;
        }
        finally
        {
            stopwatch.Stop();
            result.ElapsedTime = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Consumes a stream with graceful cancellation at a specified item count.
    /// </summary>
    public static async Task<StreamResult> ConsumeStreamWithCancellation<T>(
        IAsyncEnumerable<T> stream,
        int cancelAfterItemCount)
    {
        var result = new StreamResult();
        using var cts = new CancellationTokenSource();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await foreach (var item in stream.WithCancellation(cts.Token))
            {
                result.ItemsReceived.Add(item ?? new object());

                // Cancel after receiving specified number of items
                if (result.ItemsReceived.Count >= cancelAfterItemCount)
                {
                    cts.Cancel();
                }
            }
        }
        catch (OperationCanceledException ex)
        {
            result.StreamException = ex;
        }
        catch (Exception ex)
        {
            result.StreamException = ex;
        }
        finally
        {
            stopwatch.Stop();
            result.ElapsedTime = stopwatch.Elapsed;
        }

        return result;
    }

    /// <summary>
    /// Validates streaming behavior against expected specifications.
    /// </summary>
    public class StreamValidator
    {
        private readonly StreamResult _streamResult;
        private readonly int _expectedItemCount;
        private readonly int _maxExpectedDurationMs;

        /// <summary>
        /// Initializes a new instance of the StreamValidator.
        /// </summary>
        public StreamValidator(StreamResult streamResult, int expectedItemCount, int maxExpectedDurationMs = 10000)
        {
            _streamResult = streamResult;
            _expectedItemCount = expectedItemCount;
            _maxExpectedDurationMs = maxExpectedDurationMs;
        }

        /// <summary>
        /// Validates that the stream completed successfully.
        /// </summary>
        public bool ValidateCompletion() => _streamResult.CompletedSuccessfully;

        /// <summary>
        /// Validates that the correct number of items were received.
        /// </summary>
        public bool ValidateItemCount() => _streamResult.ItemsReceived.Count == _expectedItemCount;

        /// <summary>
        /// Validates that streaming completed within expected duration.
        /// </summary>
        public bool ValidateDuration() => _streamResult.ElapsedTime.TotalMilliseconds <= _maxExpectedDurationMs;

        /// <summary>
        /// Validates that the stream received at least the minimum items before error/timeout.
        /// </summary>
        public bool ValidateMinimumItems(int minimumCount) => _streamResult.ItemsReceived.Count >= minimumCount;

        /// <summary>
        /// Validates that an error was received.
        /// </summary>
        public bool ValidateErrorReceived() => _streamResult.StreamException != null;

        /// <summary>
        /// Validates that a timeout occurred.
        /// </summary>
        public bool ValidateTimeout() => _streamResult.TimedOut;

        /// <summary>
        /// Gets a detailed validation summary.
        /// </summary>
        public Dictionary<string, bool> GetValidationSummary() => new()
        {
            { "Completed Successfully", ValidateCompletion() },
            { "Correct Item Count", ValidateItemCount() },
            { "Within Duration", ValidateDuration() },
            { "Error Received", ValidateErrorReceived() },
            { "Timeout Occurred", ValidateTimeout() }
        };
    }

    /// <summary>
    /// Creates a mock async stream reader from an IAsyncEnumerable for testing server streaming.
    /// </summary>
    public static IAsyncStreamReader<T> CreateMockAsyncStreamReader<T>(IAsyncEnumerable<T> source) where T : class
    {
        var enumerator = source.GetAsyncEnumerator();
        var mockReader = new Mock<IAsyncStreamReader<T>>();

        mockReader
            .Setup(x => x.MoveNext(It.IsAny<CancellationToken>()))
            .Returns(async () => await enumerator.MoveNextAsync());

        mockReader
            .Setup(x => x.Current)
            .Returns(() => enumerator.Current);

        return mockReader.Object;
    }

    /// <summary>
    /// Represents a streaming operation that can be validated for performance and correctness.
    /// </summary>
    public class StreamingOperationValidator
    {
        private readonly Stopwatch _stopwatch;
        private readonly List<(int itemNumber, TimeSpan timestamp)> _itemTimestamps = [];

        /// <summary>
        /// Initializes a new instance of the StreamingOperationValidator.
        /// </summary>
        public StreamingOperationValidator()
        {
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Records receipt of an item at current time.
        /// </summary>
        public void RecordItemReceived(int itemNumber)
        {
            _itemTimestamps.Add((itemNumber, _stopwatch.Elapsed));
        }

        /// <summary>
        /// Validates that items were received in correct order.
        /// </summary>
        public bool ValidateOrderingCorrect()
        {
            for (int i = 1; i < _itemTimestamps.Count; i++)
            {
                if (_itemTimestamps[i].itemNumber <= _itemTimestamps[i - 1].itemNumber)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that streaming rate is within expected range.
        /// </summary>
        public bool ValidateStreamingRate(int expectedItemsPerSecond)
        {
            if (_itemTimestamps.Count < 2)
                return true;

            var totalTime = _stopwatch.Elapsed.TotalSeconds;
            var actualItemsPerSecond = _itemTimestamps.Count / totalTime;

            // Allow 20% variance in rate
            var tolerance = expectedItemsPerSecond * 0.2;
            return Math.Abs(actualItemsPerSecond - expectedItemsPerSecond) <= tolerance;
        }

        /// <summary>
        /// Gets the average time between item receipts.
        /// </summary>
        public TimeSpan GetAverageItemInterval()
        {
            if (_itemTimestamps.Count < 2)
                return TimeSpan.Zero;

            var totalInterval = _itemTimestamps.Last().timestamp - _itemTimestamps.First().timestamp;
            return TimeSpan.FromMilliseconds(totalInterval.TotalMilliseconds / (_itemTimestamps.Count - 1));
        }

        /// <summary>
        /// Gets a summary of streaming performance for assertions.
        /// </summary>
        public string GetSummary() =>
            $"Items received: {_itemTimestamps.Count}, Total time: {_stopwatch.Elapsed.TotalMilliseconds}ms, Avg interval: {GetAverageItemInterval().TotalMilliseconds}ms";
    }
}
