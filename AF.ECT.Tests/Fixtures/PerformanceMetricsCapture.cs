namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Helper for capturing and analyzing performance metrics during test execution.
/// Tracks execution time, memory allocation, and GC collections for benchmarking.
/// </summary>
public class PerformanceMetricsCapture : IAsyncLifetime, IDisposable
{
    private readonly Stopwatch _stopwatch;
    private GCCollectionCountSnapshot _gcStartSnapshot = null!;
    private GCCollectionCountSnapshot? _gcEndSnapshot;
    private long _startMemory;
    private long _endMemory;
    private bool _disposed;

    /// <summary>
    /// Gets the name/label for this performance capture.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets whether measurement is currently active.
    /// </summary>
    public bool IsActive => !_stopwatch.IsRunning == false;

    /// <summary>
    /// Initializes a new instance of the PerformanceMetricsCapture.
    /// </summary>
    public PerformanceMetricsCapture(string label = "Test Operation")
    {
        Label = label;
        _stopwatch = new Stopwatch();
        _gcStartSnapshot = CaptureGCCollections();
        _startMemory = GC.GetTotalMemory(false);
    }

    /// <summary>
    /// Starts the performance measurement.
    /// </summary>
    public void Start()
    {
        _startMemory = GC.GetTotalMemory(false);
        _gcStartSnapshot = CaptureGCCollections();
        _stopwatch.Restart();
    }

    /// <summary>
    /// Stops the performance measurement.
    /// </summary>
    public void Stop()
    {
        _stopwatch.Stop();
        _endMemory = GC.GetTotalMemory(false);
        _gcEndSnapshot = CaptureGCCollections();
    }

    /// <summary>
    /// Gets the total elapsed time since start.
    /// </summary>
    public TimeSpan ElapsedTime => _stopwatch.Elapsed;

    /// <summary>
    /// Gets the elapsed milliseconds.
    /// </summary>
    public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Gets the number of bytes allocated during measurement.
    /// </summary>
    public long BytesAllocated => Math.Max(0, _endMemory - _startMemory);

    /// <summary>
    /// Gets the number of Gen 0 garbage collections that occurred.
    /// </summary>
    public int Gen0Collections => _gcEndSnapshot?.Gen0 - _gcStartSnapshot.Gen0 ?? 0;

    /// <summary>
    /// Gets the number of Gen 1 garbage collections that occurred.
    /// </summary>
    public int Gen1Collections => _gcEndSnapshot?.Gen1 - _gcStartSnapshot.Gen1 ?? 0;

    /// <summary>
    /// Gets the number of Gen 2 garbage collections that occurred.
    /// </summary>
    public int Gen2Collections => _gcEndSnapshot?.Gen2 - _gcStartSnapshot.Gen2 ?? 0;

    /// <summary>
    /// Gets the total number of garbage collections.
    /// </summary>
    public int TotalCollections => Gen0Collections + Gen1Collections + Gen2Collections;

    /// <summary>
    /// Gets metrics as a structured dictionary.
    /// </summary>
    public Dictionary<string, object> GetMetrics() => new()
    {
        { "Label", Label },
        { "ElapsedTime", $"{ElapsedTime.TotalMilliseconds:F2}ms" },
        { "ElapsedSeconds", $"{ElapsedTime.TotalSeconds:F4}s" },
        { "BytesAllocated", FormatBytes(BytesAllocated) },
        { "Gen0Collections", Gen0Collections },
        { "Gen1Collections", Gen1Collections },
        { "Gen2Collections", Gen2Collections },
        { "TotalCollections", TotalCollections }
    };

    /// <summary>
    /// Gets a performance summary as formatted string.
    /// </summary>
    public string GetSummary() =>
        $"{Label}: {ElapsedTime.TotalMilliseconds:F2}ms, {FormatBytes(BytesAllocated)}, GC: {TotalCollections}";

    /// <summary>
    /// Asserts that performance is within acceptable thresholds.
    /// </summary>
    public PerformanceAssertions Should() => new(this);

    /// <summary>
    /// Initializes asynchronously (starts measurement).
    /// </summary>
    public Task InitializeAsync()
    {
        Start();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes asynchronously (stops measurement).
    /// </summary>
    public Task DisposeAsync()
    {
        Stop();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronously disposes resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _stopwatch.Stop();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Snapshot of GC collection counts.
    /// </summary>
    private class GCCollectionCountSnapshot
    {
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
    }

    /// <summary>
    /// Captures current GC collection counts.
    /// </summary>
    private static GCCollectionCountSnapshot CaptureGCCollections() => new()
    {
        Gen0 = GC.CollectionCount(0),
        Gen1 = GC.CollectionCount(1),
        Gen2 = GC.CollectionCount(2)
    };

    /// <summary>
    /// Formats a byte count into human-readable format.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:F2} {sizes[order]}";
    }

    /// <summary>
    /// Fluent assertions for performance validation.
    /// </summary>
    public class PerformanceAssertions
    {
        private readonly PerformanceMetricsCapture _metrics;

        /// <summary>
        /// Initializes a new instance of PerformanceAssertions.
        /// </summary>
        public PerformanceAssertions(PerformanceMetricsCapture metrics)
        {
            _metrics = metrics;
        }

        /// <summary>
        /// Asserts that execution completed within specified milliseconds.
        /// </summary>
        public PerformanceAssertions CompletedWithin(int milliseconds)
        {
            _metrics.ElapsedMilliseconds.Should().BeLessThanOrEqualTo(milliseconds,
                because: $"execution should complete within {milliseconds}ms but took {_metrics.ElapsedMilliseconds}ms");
            return this;
        }

        /// <summary>
        /// Asserts that memory allocation was below threshold.
        /// </summary>
        public PerformanceAssertions AllocatedLessThan(long bytes)
        {
            _metrics.BytesAllocated.Should().BeLessThanOrEqualTo(bytes,
                because: $"allocation should be less than {FormatBytes(bytes)} but was {FormatBytes(_metrics.BytesAllocated)}");
            return this;
        }

        /// <summary>
        /// Asserts that no Gen 2 collections occurred.
        /// </summary>
        public PerformanceAssertions WithoutGen2Collection()
        {
            _metrics.Gen2Collections.Should().Be(0,
                because: "Gen 2 collections indicate significant memory pressure");
            return this;
        }

        /// <summary>
        /// Asserts that GC collections stayed below threshold.
        /// </summary>
        public PerformanceAssertions WithMaximumCollections(int maxCollections)
        {
            _metrics.TotalCollections.Should().BeLessThanOrEqualTo(maxCollections,
                because: $"GC collections ({_metrics.TotalCollections}) should not exceed {maxCollections}");
            return this;
        }

        /// <summary>
        /// Asserts that memory was within expected range.
        /// </summary>
        public PerformanceAssertions WithMemoryBetween(long minBytes, long maxBytes)
        {
            _metrics.BytesAllocated.Should().BeGreaterThanOrEqualTo(minBytes,
                because: $"allocation should be at least {FormatBytes(minBytes)}");
            _metrics.BytesAllocated.Should().BeLessThanOrEqualTo(maxBytes,
                because: $"allocation should not exceed {FormatBytes(maxBytes)}");
            return this;
        }

        /// <summary>
        /// Provides method chaining completion and returns metrics.
        /// </summary>
        public PerformanceMetricsCapture And => _metrics;
    }
}
