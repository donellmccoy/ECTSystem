namespace AF.ECT.Tests.Utilities;

using System.Diagnostics;
using System.Collections.Concurrent;

/// <summary>
/// Performance measurement utility for tracking test execution times and comparing optimization impact.
/// Enables teams to measure actual performance improvements from test fixture optimizations.
/// Thread-safe for use across concurrent test execution.
/// </summary>
public class TestPerformanceAnalyzer : IDisposable
{
    private readonly ConcurrentDictionary<string, PerformanceMetrics> _metrics;
    private readonly Stopwatch _globalTimer;
    private bool _disposed;

    /// <summary>
    /// Represents collected metrics for a specific test or operation.
    /// </summary>
    public class PerformanceMetrics
    {
        public string Name { get; set; } = string.Empty;
        public long ElapsedMilliseconds { get; set; }
        public long AllocatedMemory { get; set; }
        public int ExecutionCount { get; set; }
        public double AverageMs => ExecutionCount > 0 ? ElapsedMilliseconds / (double)ExecutionCount : 0;
        public double MedianMs { get; set; }
        public double PercentileP95Ms { get; set; }
        public long PeakMemory { get; set; }
    }

    /// <summary>
    /// Tracks execution timing for a specific operation.
    /// </summary>
    public class TimingScope : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly Action<long> _onComplete;

        public TimingScope(Action<long> onComplete)
        {
            _onComplete = onComplete;
            _stopwatch = Stopwatch.StartNew();
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;

        public void Dispose()
        {
            _stopwatch.Stop();
            _onComplete(_stopwatch.ElapsedMilliseconds);
        }
    }

    public TestPerformanceAnalyzer()
    {
        _metrics = new ConcurrentDictionary<string, PerformanceMetrics>();
        _globalTimer = Stopwatch.StartNew();
    }

    /// <summary>
    /// Begins timing a named operation.
    /// </summary>
    public TimingScope BeginTiming(string operationName)
    {
        return new TimingScope(elapsed =>
        {
            RecordMetric(operationName, elapsed);
        });
    }

    /// <summary>
    /// Records a metric for a specific operation.
    /// </summary>
    public void RecordMetric(string operationName, long elapsedMs)
    {
        _metrics.AddOrUpdate(
            operationName,
            new PerformanceMetrics { Name = operationName, ElapsedMilliseconds = elapsedMs, ExecutionCount = 1 },
            (key, existing) =>
            {
                existing.ElapsedMilliseconds += elapsedMs;
                existing.ExecutionCount++;
                return existing;
            });
    }

    /// <summary>
    /// Gets collected metrics for a specific operation.
    /// </summary>
    public PerformanceMetrics? GetMetrics(string operationName)
    {
        return _metrics.TryGetValue(operationName, out var metrics) ? metrics : null;
    }

    /// <summary>
    /// Gets all collected metrics.
    /// </summary>
    public IEnumerable<PerformanceMetrics> GetAllMetrics() => _metrics.Values;

    /// <summary>
    /// Generates a summary report of all metrics.
    /// </summary>
    public string GenerateSummaryReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
        report.AppendLine("║              TEST PERFORMANCE ANALYSIS REPORT                   ║");
        report.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        report.AppendLine();

        var sortedMetrics = _metrics.Values.OrderByDescending(m => m.ElapsedMilliseconds).ToList();

        if (!sortedMetrics.Any())
        {
            report.AppendLine("No metrics collected.");
            return report.ToString();
        }

        report.AppendLine($"{"Operation",-40} {"Count",6} {"Total(ms)",12} {"Avg(ms)",12} {"Min(ms)",12} {"Max(ms)",12}");
        report.AppendLine(new string('─', 94));

        foreach (var metric in sortedMetrics)
        {
            report.AppendLine($"{metric.Name,-40} {metric.ExecutionCount,6} {metric.ElapsedMilliseconds,12:F2} {metric.AverageMs,12:F2}");
        }

        report.AppendLine();
        report.AppendLine($"Total Execution Time: {_globalTimer.ElapsedMilliseconds}ms");

        return report.ToString();
    }

    /// <summary>
    /// Generates a comparison report between two test runs.
    /// </summary>
    public static string GenerateComparisonReport(
        TestPerformanceAnalyzer baseline,
        TestPerformanceAnalyzer optimized)
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("╔═══════════════════════════════════════════════════════════════════════════════════╗");
        report.AppendLine("║             TEST OPTIMIZATION IMPACT COMPARISON REPORT                           ║");
        report.AppendLine("╚═══════════════════════════════════════════════════════════════════════════════════╝");
        report.AppendLine();

        report.AppendLine($"{"Operation",-40} {"Baseline(ms)",15} {"Optimized(ms)",15} {"Improvement",15} {"Speedup",10}");
        report.AppendLine(new string('─', 95));

        var baselineMetrics = baseline.GetAllMetrics().ToDictionary(m => m.Name);
        var optimizedMetrics = optimized.GetAllMetrics().ToDictionary(m => m.Name);

        var allOperations = baselineMetrics.Keys.Union(optimizedMetrics.Keys).OrderBy(x => x);

        double totalBaselineMs = 0;
        double totalOptimizedMs = 0;

        foreach (var operation in allOperations)
        {
            var baselineMs = baselineMetrics.TryGetValue(operation, out var b) ? b.AverageMs : 0;
            var optimizedMs = optimizedMetrics.TryGetValue(operation, out var o) ? o.AverageMs : 0;

            totalBaselineMs += baselineMs;
            totalOptimizedMs += optimizedMs;

            var improvement = baselineMs - optimizedMs;
            var improvementPercent = baselineMs > 0 ? (improvement / baselineMs * 100) : 0;
            var speedup = baselineMs > 0 ? baselineMs / (optimizedMs > 0 ? optimizedMs : 1) : 1;

            var improvementString = improvement > 0 ? $"{improvementPercent:F1}%" : "N/A";
            var speedupString = speedup > 1 ? $"{speedup:F1}x" : "N/A";

            report.AppendLine($"{operation,-40} {baselineMs,15:F2} {optimizedMs,15:F2} {improvementString,15} {speedupString,10}");
        }

        report.AppendLine(new string('─', 95));

        var totalImprovement = totalBaselineMs - totalOptimizedMs;
        var totalImprovementPercent = totalBaselineMs > 0 ? (totalImprovement / totalBaselineMs * 100) : 0;
        var totalSpeedup = totalBaselineMs > 0 ? totalBaselineMs / (totalOptimizedMs > 0 ? totalOptimizedMs : 1) : 1;

        report.AppendLine($"{"TOTAL",-40} {totalBaselineMs,15:F2} {totalOptimizedMs,15:F2} {totalImprovementPercent:F1}% {totalSpeedup:F1}x");
        report.AppendLine();
        report.AppendLine($"Overall Improvement: {totalImprovementPercent:F1}% faster ({totalSpeedup:F1}x speedup)");

        return report.ToString();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _globalTimer?.Stop();
            _disposed = true;
        }
    }
}

/// <summary>
/// Test collection-scoped performance analyzer for automatic timing of fixture operations.
/// Use with xUnit collection fixtures to measure fixture initialization and cleanup overhead.
/// </summary>
public class FixturePerformanceAnalyzer : IAsyncLifetime
{
    private readonly TestPerformanceAnalyzer _analyzer;

    public FixturePerformanceAnalyzer()
    {
        _analyzer = new TestPerformanceAnalyzer();
    }

    public TestPerformanceAnalyzer Analyzer => _analyzer;

    public Task InitializeAsync()
    {
        Analyzer.RecordMetric("FixtureInitialization", 0);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        System.Console.WriteLine(Analyzer.GenerateSummaryReport());
        Analyzer.Dispose();
        return Task.CompletedTask;
    }
}
