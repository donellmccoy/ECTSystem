namespace AF.ECT.Tests.Unit;

using BenchmarkDotNet.Attributes;
using FluentAssertions;

/// <summary>
/// Contains performance benchmark tests using BenchmarkDotNet.
/// Measures execution performance and detects performance regressions.
/// Note: Run separately with: dotnet run -c Release --project AF.ECT.Tests
/// </summary>
[MemoryDiagnoser]
public class PerformanceBenchmarkTests
{
    private List<int> _testData = null!;

    /// <summary>
    /// Setup method called before each benchmark.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _testData = Enumerable.Range(0, 10000).ToList();
    }

    /// <summary>
    /// Benchmarks LINQ Where performance.
    /// </summary>
    [Benchmark]
    public int BenchmarkLinqWhere()
    {
        return _testData.Where(x => x % 2 == 0).Count();
    }

    /// <summary>
    /// Benchmarks manual loop performance vs LINQ.
    /// </summary>
    [Benchmark]
    public int BenchmarkManualLoop()
    {
        var count = 0;
        foreach (var item in _testData)
        {
            if (item % 2 == 0)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Benchmarks string concatenation performance.
    /// </summary>
    [Benchmark]
    public string BenchmarkStringConcatenation()
    {
        var result = "";
        for (int i = 0; i < 100; i++)
        {
            result += $"Item {i}, ";
        }
        return result;
    }

    /// <summary>
    /// Benchmarks StringBuilder performance vs string concatenation.
    /// </summary>
    [Benchmark]
    public string BenchmarkStringBuilder()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 100; i++)
        {
            sb.Append($"Item {i}, ");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Benchmarks dictionary lookup performance.
    /// </summary>
    [Benchmark]
    public int BenchmarkDictionaryLookup()
    {
        var dict = _testData.ToDictionary(x => x, x => x * 2);
        var sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (dict.TryGetValue(i, out var value))
                sum += value;
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks list binary search performance.
    /// </summary>
    [Benchmark]
    public int BenchmarkBinarySearch()
    {
        var sorted = _testData.OrderBy(x => x).ToList();
        var sum = 0;
        for (int i = 0; i < 100; i++)
        {
            var index = sorted.BinarySearch(i * 100);
            if (index >= 0)
                sum++;
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks array access vs list access.
    /// </summary>
    [Benchmark]
    public long BenchmarkArrayAccess()
    {
        var array = _testData.ToArray();
        long sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i];
        }
        return sum;
    }

    /// <summary>
    /// Benchmarks async/await overhead.
    /// </summary>
    [Benchmark]
    public async Task<int> BenchmarkAsyncOverhead()
    {
        var results = new List<int>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(await Task.FromResult(i));
        }
        return results.Count;
    }

    /// <summary>
    /// Benchmarks task allocation overhead.
    /// </summary>
    [Benchmark]
    public int BenchmarkTaskAllocation()
    {
        var tasks = new List<Task<int>>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.FromResult(i));
        }
        return tasks.Count;
    }

    /// <summary>
    /// Benchmarks JSON serialization performance.
    /// </summary>
    [Benchmark]
    public string BenchmarkJsonSerialization()
    {
        var testObj = new { Values = _testData.Take(100).ToList() };
        return System.Text.Json.JsonSerializer.Serialize(testObj);
    }
}

/// <summary>
/// Non-benchmark performance regression detection tests.
/// </summary>
[Collection("Performance Regression Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "Performance")]
public class PerformanceRegressionTests
{
    private const int TargetMaxMs = 100;
    private const int TargetMaxMemoryMb = 50;

    /// <summary>
    /// Tests that simple list operations complete within time budget.
    /// </summary>
    [Fact]
    public void ListOperations_CompleteWithinTimeBudget()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var list = new List<int>();

        // Act
        for (int i = 0; i < 100000; i++)
        {
            list.Add(i);
        }
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(TargetMaxMs,
            $"List operations should complete within {TargetMaxMs}ms");
    }

    /// <summary>
    /// Tests that LINQ operations meet performance targets.
    /// </summary>
    [Fact]
    public void LinqOperations_MeetPerformanceTargets()
    {
        // Arrange
        var data = Enumerable.Range(0, 100000).ToList();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var result = data
            .Where(x => x % 2 == 0)
            .Select(x => x * 2)
            .OrderBy(x => x)
            .Take(1000)
            .ToList();

        sw.Stop();

        // Assert
        result.Should().HaveCount(1000);
        sw.ElapsedMilliseconds.Should().BeLessThan(TargetMaxMs,
            $"LINQ operations should complete within {TargetMaxMs}ms");
    }

    /// <summary>
    /// Tests that string operations don't exceed memory budget.
    /// </summary>
    [Fact]
    public void StringOperations_RemainWithinMemoryBudget()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var strings = new List<string>();
        for (int i = 0; i < 10000; i++)
        {
            strings.Add($"String {i}: This is a test string with some content");
        }

        // Assert
        var finalMemory = GC.GetTotalMemory(false);
        var usedMemoryMb = (finalMemory - initialMemory) / (1024 * 1024);
        usedMemoryMb.Should().BeLessThan(TargetMaxMemoryMb,
            $"String operations should use less than {TargetMaxMemoryMb}MB");
    }

    /// <summary>
    /// Tests dictionary lookup performance consistency.
    /// </summary>
    [Fact]
    public void DictionaryLookup_HasConsistentPerformance()
    {
        // Arrange
        var dict = Enumerable.Range(0, 10000)
            .ToDictionary(x => x, x => x.ToString());
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var sum = 0;
        for (int i = 0; i < 10000; i++)
        {
            if (dict.TryGetValue(i, out _))
                sum++;
        }

        sw.Stop();

        // Assert
        sum.Should().Be(10000);
        sw.ElapsedMilliseconds.Should().BeLessThan(TargetMaxMs,
            $"Dictionary lookups should complete within {TargetMaxMs}ms");
    }

    /// <summary>
    /// Tests that async operations don't have excessive overhead.
    /// </summary>
    [Fact]
    public async Task AsyncOperations_HaveMinimalOverhead()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Delay(1));
        }
        await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(TargetMaxMs,
            $"Async operations should complete within {TargetMaxMs}ms");
    }

    /// <summary>
    /// Tests that garbage collection doesn't cause excessive pauses.
    /// </summary>
    [Fact]
    public void GarbageCollection_DoesNotCauseLongPauses()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var initialGen0 = GC.CollectionCount(0);

        // Act
        var allocations = new List<byte[]>();
        for (int i = 0; i < 1000; i++)
        {
            allocations.Add(new byte[10000]);
        }
        allocations.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(1000,
            "GC should not cause excessive pauses");
    }

    /// <summary>
    /// Tests that concurrent operations maintain performance.
    /// </summary>
    [Fact]
    public async Task ConcurrentOperations_MaintainPerformance()
    {
        // Arrange
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                var sum = 0;
                for (int j = 0; j < 1000; j++)
                {
                    sum += j;
                    if (j % 100 == 0)
                        await Task.Yield();
                }
            }));
        }
        await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(5000,
            "Concurrent operations should maintain reasonable performance");
    }
}
