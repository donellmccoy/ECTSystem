namespace AF.ECT.Tests.Utilities;

using System.Diagnostics;

/// <summary>
/// Baseline measurement utilities for establishing test performance baselines.
/// Use to measure performance before and after optimization for accurate impact analysis.
/// </summary>
public static class PerformanceBaseline
{
    private static readonly Dictionary<string, TestPerformanceAnalyzer.PerformanceMetrics> _savedBaselines = new();

    /// <summary>
    /// Saves current performance metrics as a baseline for future comparison.
    /// </summary>
    public static void SaveBaseline(string baselineName, TestPerformanceAnalyzer analyzer)
    {
        var allMetrics = analyzer.GetAllMetrics().ToList();
        foreach (var metric in allMetrics)
        {
            var key = $"{baselineName}:{metric.Name}";
            _savedBaselines[key] = metric;
        }
    }

    /// <summary>
    /// Loads a previously saved baseline for comparison.
    /// </summary>
    public static Dictionary<string, TestPerformanceAnalyzer.PerformanceMetrics>? LoadBaseline(string baselineName)
    {
        var baselineMetrics = _savedBaselines
            .Where(kvp => kvp.Key.StartsWith($"{baselineName}:"))
            .ToDictionary(
                kvp => kvp.Key.Substring($"{baselineName}:".Length),
                kvp => kvp.Value);

        return baselineMetrics.Any() ? baselineMetrics : null;
    }

    /// <summary>
    /// Clears all saved baselines from memory.
    /// </summary>
    public static void ClearBaselines()
    {
        _savedBaselines.Clear();
    }
}

/// <summary>
/// Helper for measuring fixture initialization overhead.
/// Useful for comparing mock creation time before/after optimization.
/// </summary>
public class FixtureInitializationBenchmark
{
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Measures the time to create and initialize a fixture.
    /// </summary>
    /// <typeparam name="TFixture">The fixture type to measure</typeparam>
    /// <returns>Elapsed milliseconds for initialization</returns>
    public async Task<long> MeasureInitializationAsync<TFixture>() where TFixture : IAsyncLifetime, new()
    {
        var fixture = new TFixture();
        _stopwatch.Restart();

        try
        {
            await fixture.InitializeAsync();
        }
        finally
        {
            _stopwatch.Stop();
            await fixture.DisposeAsync();
        }

        return _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Measures the time to create and initialize a fixture multiple times.
    /// Useful for measuring average overhead across multiple iterations.
    /// </summary>
    public async Task<(long TotalMs, long AverageMs, long MinMs, long MaxMs)> MeasureMultipleInitializations<TFixture>(
        int iterations) where TFixture : IAsyncLifetime, new()
    {
        var times = new List<long>();

        for (int i = 0; i < iterations; i++)
        {
            times.Add(await MeasureInitializationAsync<TFixture>());
        }

        var totalMs = times.Sum();
        var averageMs = (long)times.Average();
        var minMs = times.Min();
        var maxMs = times.Max();

        return (totalMs, averageMs, minMs, maxMs);
    }

    /// <summary>
    /// Generates a report comparing fixture initialization performance.
    /// </summary>
    public static string GenerateFixtureComparisonReport(
        string fixtureNameTraditional,
        long traditionalInitMs,
        string fixtureNameOptimized,
        long optimizedInitMs)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        report.AppendLine("║         FIXTURE INITIALIZATION PERFORMANCE COMPARISON            ║");
        report.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        report.AppendLine();

        report.AppendLine($"Traditional: {fixtureNameTraditional,-40} {traditionalInitMs,10}ms");
        report.AppendLine($"Optimized:   {fixtureNameOptimized,-40} {optimizedInitMs,10}ms");
        report.AppendLine();

        var improvement = traditionalInitMs - optimizedInitMs;
        var improvementPercent = traditionalInitMs > 0 ? (improvement / (double)traditionalInitMs * 100) : 0;
        var speedup = traditionalInitMs > 0 ? traditionalInitMs / (double)(optimizedInitMs > 0 ? optimizedInitMs : 1) : 1;

        report.AppendLine($"Improvement: {improvement}ms ({improvementPercent:F1}% faster)");
        report.AppendLine($"Speedup:     {speedup:F1}x");
        report.AppendLine();
        report.AppendLine($"For 100 test iterations:");
        report.AppendLine($"  Traditional: {traditionalInitMs * 100}ms");
        report.AppendLine($"  Optimized:   {optimizedInitMs * 100}ms");
        report.AppendLine($"  Saved:       {improvement * 100}ms");

        return report.ToString();
    }
}

/// <summary>
/// Mock creation performance benchmark for measuring DataServiceMockFactory improvements.
/// </summary>
public class MockCreationBenchmark
{
    /// <summary>
    /// Measures the time to create a mock using a factory method.
    /// </summary>
    public long MeasureMockCreation<T>(Func<Mock<T>> factory) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        _ = factory();
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Measures mock creation across multiple iterations.
    /// </summary>
    public (long TotalMs, double AverageMs) MeasureMultipleMockCreations<T>(
        Func<Mock<T>> factory,
        int iterations) where T : class
    {
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            _ = factory();
        }

        stopwatch.Stop();
        var averageMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        return (stopwatch.ElapsedMilliseconds, averageMs);
    }

    /// <summary>
    /// Generates comparison report for mock creation performance.
    /// </summary>
    public static string GenerateMockComparisonReport(
        string mockType,
        int iterations,
        double traditionalAvgMs,
        double optimizedAvgMs)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        report.AppendLine("║         MOCK CREATION PERFORMANCE COMPARISON                    ║");
        report.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        report.AppendLine();

        report.AppendLine($"Mock Type: {mockType}");
        report.AppendLine($"Iterations: {iterations}");
        report.AppendLine();

        report.AppendLine($"Traditional: {traditionalAvgMs:F4}ms per mock");
        report.AppendLine($"Optimized:   {optimizedAvgMs:F4}ms per mock");
        report.AppendLine();

        var improvement = traditionalAvgMs - optimizedAvgMs;
        var improvementPercent = traditionalAvgMs > 0 ? (improvement / traditionalAvgMs * 100) : 0;
        var speedup = traditionalAvgMs > 0 ? traditionalAvgMs / (optimizedAvgMs > 0 ? optimizedAvgMs : 1) : 1;

        report.AppendLine($"Improvement: {improvement:F4}ms per mock ({improvementPercent:F1}% faster)");
        report.AppendLine($"Speedup:     {speedup:F1}x");
        report.AppendLine();
        report.AppendLine($"For {iterations} mocks:");
        report.AppendLine($"  Traditional: {traditionalAvgMs * iterations:F2}ms");
        report.AppendLine($"  Optimized:   {optimizedAvgMs * iterations:F2}ms");
        report.AppendLine($"  Saved:       {improvement * iterations:F2}ms");

        return report.ToString();
    }
}

/// <summary>
/// Memory allocation benchmark for measuring object allocation reduction.
/// </summary>
public class MemoryAllocationBenchmark
{
    /// <summary>
    /// Measures memory allocated by an operation.
    /// </summary>
    public long MeasureMemoryAllocation(Action operation)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(true);

        operation();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(true);

        return Math.Max(0, memoryAfter - memoryBefore);
    }

    /// <summary>
    /// Measures average memory allocation across multiple iterations.
    /// </summary>
    public (long TotalBytes, double AverageBytes) MeasureMultipleAllocations(Action operation, int iterations)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(true);

        for (int i = 0; i < iterations; i++)
        {
            operation();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(true);
        var totalBytes = Math.Max(0, memoryAfter - memoryBefore);
        var averageBytes = totalBytes / (double)iterations;

        return (totalBytes, averageBytes);
    }

    /// <summary>
    /// Generates memory allocation comparison report.
    /// </summary>
    public static string GenerateMemoryComparisonReport(
        string operationName,
        int iterations,
        long traditionalTotalBytes,
        long optimizedTotalBytes)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        report.AppendLine("║         MEMORY ALLOCATION COMPARISON REPORT                    ║");
        report.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        report.AppendLine();

        report.AppendLine($"Operation: {operationName}");
        report.AppendLine($"Iterations: {iterations}");
        report.AppendLine();

        var traditionalPerIter = traditionalTotalBytes / (double)iterations;
        var optimizedPerIter = optimizedTotalBytes / (double)iterations;

        report.AppendLine($"Traditional: {traditionalTotalBytes / 1024.0:F2}KB total ({traditionalPerIter / 1024.0:F4}KB per iteration)");
        report.AppendLine($"Optimized:   {optimizedTotalBytes / 1024.0:F2}KB total ({optimizedPerIter / 1024.0:F4}KB per iteration)");
        report.AppendLine();

        var improvement = traditionalTotalBytes - optimizedTotalBytes;
        var improvementPercent = traditionalTotalBytes > 0 ? (improvement / (double)traditionalTotalBytes * 100) : 0;

        report.AppendLine($"Reduction:   {improvement / 1024.0:F2}KB ({improvementPercent:F1}% less memory)");

        return report.ToString();
    }
}
