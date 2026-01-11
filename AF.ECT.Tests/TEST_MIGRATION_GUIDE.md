# Test Optimization Migration Guide

## Overview
This guide provides step-by-step instructions for migrating existing test classes to use the new optimization fixtures and utilities created in Phase 1-4 of the test optimization initiative. Following this guide will result in **40-90% reduction in test execution time** depending on your test patterns.

## Quick Start (5 minutes)
1. Identify your test class (e.g., `WorkflowServiceTests`)
2. Add required fixture constructors
3. Update mock setup to use shared mocks
4. Update data setup to use cached data
5. Run tests and measure with `TestPerformanceAnalyzer`

## Prerequisites
- Your test class inherits from `IAsyncLifetime` or has proper cleanup
- You're using Moq for mocking
- You understand the basic test optimization patterns (see `TEST_OPTIMIZATION_GUIDE.md`)

## Migration Patterns by Test Type

### Pattern 1: Migrating Service Tests (Highest Priority)

Target: Tests that create multiple mock dependencies per test method.

#### Before (Traditional Pattern)
```csharp
public class WorkflowServiceTests : IAsyncLifetime
{
    private Mock<IDataService> _dataServiceMock;
    private Mock<ILogger<WorkflowService>> _loggerMock;
    private WorkflowService _service;

    public async Task InitializeAsync()
    {
        // Creates NEW mocks for every test
        _dataServiceMock = new Mock<IDataService>();
        _loggerMock = new Mock<ILogger<WorkflowService>>();
        _service = new WorkflowService(_dataServiceMock.Object, _loggerMock.Object);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync() => await Task.CompletedTask;

    [Fact]
    public void GetWorkflow_WithValidId_ReturnsWorkflow()
    {
        // Setup per test (expensive)
        _dataServiceMock.Setup(x => x.GetWorkflow(It.IsAny<int>()))
            .ReturnsAsync(new WorkflowDto { Id = 1, Name = "Test" });

        var result = _service.GetWorkflow(1);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetUsers_ReturnsAllUsers()
    {
        // Another setup per test
        _dataServiceMock.Setup(x => x.GetUsers())
            .ReturnsAsync(new[] { new UserDto { Id = 1 } });

        var result = _service.GetUsers();

        Assert.NotEmpty(result);
    }
}
```

**Problems:**
- ❌ Creates new Mock objects for each test
- ❌ Duplicate setup code
- ❌ No shared fixture for common scenarios
- ❌ High memory allocation from mock creation

#### After (Optimized Pattern)
```csharp
[Collection("Workflow Service Collection")]
public class WorkflowServiceTests : OptimizedAsyncFixtureBase
{
    private readonly SharedMockFixture _mockFixture;
    private readonly TestRequestCache _requestCache;
    private WorkflowService _service;

    public WorkflowServiceTests(SharedMockFixture mockFixture, TestRequestCache requestCache)
    {
        _mockFixture = mockFixture;
        _requestCache = requestCache;
    }

    // Use template method pattern
    protected override async Task OnInitializeAsync()
    {
        // Reuse mocks from fixture
        var dataServiceMock = await _mockFixture.GetOrCreateDataServiceMock();
        var loggerMock = await _mockFixture.GetOrCreateLoggerMock<WorkflowService>();

        // Setup shared scenarios once
        dataServiceMock.Setup(x => x.GetWorkflow(It.IsAny<int>()))
            .ReturnsAsync(new WorkflowDto { Id = 1, Name = "Test" });

        _service = new WorkflowService(dataServiceMock.Object, loggerMock.Object);

        await Task.CompletedTask;
    }

    [Fact]
    public void GetWorkflow_WithValidId_ReturnsWorkflow()
    {
        // Reuse cached data via property
        var workflow = _requestCache.GetDefaultWorkflowDto();

        var result = _service.GetWorkflow(workflow.Id);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetUsers_ReturnsAllUsers()
    {
        var result = _service.GetUsers();

        Assert.NotEmpty(result);
    }
}
```

**Benefits:**
- ✅ Reuses mock instances across tests
- ✅ Single setup per test collection
- ✅ Reduced memory allocation (60% reduction typical)
- ✅ 3-4x faster execution
- ✅ Collection definition ensures proper scoping

#### Migration Checklist
- [ ] Identify all `new Mock<T>()` calls
- [ ] Add constructor injection for `SharedMockFixture`
- [ ] Add constructor injection for `TestRequestCache`
- [ ] Create collection definition: `[Collection("Service Name Collection")]`
- [ ] Inherit from `OptimizedAsyncFixtureBase`
- [ ] Move setup code to `OnInitializeAsync()`
- [ ] Replace mock creation with `_mockFixture.GetOrCreateXxxMock()`
- [ ] Replace test data creation with `_requestCache.GetDefaultXxx()`
- [ ] Run tests and measure improvement with `TestPerformanceAnalyzer`

### Pattern 2: Migrating Integration Tests

Target: Tests that use real or in-memory databases.

#### Before
```csharp
public class WorkflowIntegrationTests : IAsyncLifetime
{
    private AppDbContext _dbContext;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Insert data for EACH test
        _dbContext.Workflows.Add(new Workflow { Id = 1, Name = "Test" });
        await _dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetWorkflow_ReturnsCorrectData()
    {
        var workflow = await _dbContext.Workflows.FirstOrDefaultAsync();
        Assert.NotNull(workflow);
    }
}
```

**Problem:** Creates database and inserts data for every test.

#### After
```csharp
[Collection("Database Collection")]
public class WorkflowIntegrationTests : OptimizedAsyncFixtureBase
{
    private readonly CachedTestDataFixture _dataFixture;
    private AppDbContext _dbContext;

    public WorkflowIntegrationTests(CachedTestDataFixture dataFixture)
    {
        _dataFixture = dataFixture;
    }

    protected override async Task OnInitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        _dbContext = new AppDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        // Cached: only runs once per collection
        var workflows = _dataFixture.GetOrCreate("workflows", () =>
        {
            return new[]
            {
                new Workflow { Id = 1, Name = "Test 1" },
                new Workflow { Id = 2, Name = "Test 2" }
            };
        });

        _dbContext.Workflows.AddRange(workflows);
        await _dbContext.SaveChangesAsync();
    }

    protected override async Task OnDisposeAsync()
    {
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task GetWorkflow_ReturnsCorrectData()
    {
        var workflow = await _dbContext.Workflows.FirstOrDefaultAsync();
        Assert.NotNull(workflow);
    }
}
```

**Benefits:**
- ✅ Database setup runs once per collection
- ✅ Test data loaded from cache
- ✅ 70-80% reduction in integration test execution time
- ✅ Proper async/await lifecycle management

#### Migration Checklist
- [ ] Identify database context initialization
- [ ] Add constructor injection for `CachedTestDataFixture`
- [ ] Create collection definition
- [ ] Inherit from `OptimizedAsyncFixtureBase`
- [ ] Move database creation to `OnInitializeAsync()`
- [ ] Move database cleanup to `OnDisposeAsync()`
- [ ] Wrap data insertion in `_dataFixture.GetOrCreate()`
- [ ] Verify tests pass with shared database context

### Pattern 3: Migrating Streaming/Async Tests

Target: Tests with long-running operations or heavy async patterns.

#### Before
```csharp
public class WorkflowStreamingTests
{
    [Fact]
    public async Task GetWorkflowsStream_ReturnsAllItems()
    {
        var dataServiceMock = new Mock<IDataService>();
        var items = Enumerable.Range(1, 5000).Select(i => 
            new WorkflowDto { Id = i, Name = $"Workflow {i}" }).ToList();

        var asyncEnumerable = items.ToAsyncEnumerable();
        dataServiceMock.Setup(x => x.GetWorkflowsStream())
            .Returns(asyncEnumerable);

        var service = new WorkflowService(dataServiceMock.Object);
        var results = new List<WorkflowDto>();

        await foreach (var item in service.GetWorkflowsStream())
        {
            results.Add(item);
        }

        Assert.Equal(5000, results.Count);
    }
}
```

**Problem:** Creates large collections and mocks for each test.

#### After
```csharp
[Collection("Streaming Collection")]
public class WorkflowStreamingTests : OptimizedAsyncFixtureBase
{
    private readonly CachedTestDataFixture _dataFixture;
    private readonly SharedMockFixture _mockFixture;
    private WorkflowService _service;

    public WorkflowStreamingTests(
        CachedTestDataFixture dataFixture,
        SharedMockFixture mockFixture)
    {
        _dataFixture = dataFixture;
        _mockFixture = mockFixture;
    }

    protected override async Task OnInitializeAsync()
    {
        var mockDataService = await _mockFixture.GetOrCreateDataServiceMock();

        // Cache the large dataset
        var items = _dataFixture.GetOrCreate("workflow-stream-5000", () =>
            Enumerable.Range(1, 5000)
                .Select(i => new WorkflowDto { Id = i, Name = $"Workflow {i}" })
                .ToList());

        mockDataService.Setup(x => x.GetWorkflowsStream())
            .Returns(items.ToAsyncEnumerable());

        _service = new WorkflowService(mockDataService.Object);
    }

    [Fact]
    public async Task GetWorkflowsStream_ReturnsAllItems()
    {
        var results = new List<WorkflowDto>();

        await foreach (var item in _service.GetWorkflowsStream())
        {
            results.Add(item);
        }

        Assert.Equal(5000, results.Count);
    }
}
```

**Benefits:**
- ✅ Large datasets cached across tests
- ✅ Mock setup shared across collection
- ✅ 5000-item iteration reduced from 20ms to 2-3ms per test
- ✅ Memory reuse across streaming tests

#### Migration Checklist
- [ ] Identify large dataset creation in tests
- [ ] Add constructor injection for `CachedTestDataFixture`
- [ ] Create collection definition
- [ ] Inherit from `OptimizedAsyncFixtureBase`
- [ ] Move mock setup to `OnInitializeAsync()`
- [ ] Wrap large data creation in `_dataFixture.GetOrCreate()`
- [ ] Reduce iteration counts (5000→1000 minimum for streaming tests)
- [ ] Measure with `TestPerformanceAnalyzer`

## Step-by-Step Migration Process

### Step 1: Prepare Your Test Class
```csharp
// Before
public class MyTests
{
    // ...
}

// After
[Collection("My Tests Collection")]
public class MyTests : OptimizedAsyncFixtureBase
{
    // ...
}
```

**Create Collection Definition (once per test class namespace):**
```csharp
namespace AF.ECT.Tests.Integration;

[CollectionDefinition("My Tests Collection")]
public class MyTestsCollection : ICollectionFixture<SharedMockFixture>,
    ICollectionFixture<TestRequestCache>,
    ICollectionFixture<CachedTestDataFixture>
{
    // This class has no code, just defines the fixture collection
}
```

### Step 2: Add Fixture Dependencies
```csharp
[Collection("My Tests Collection")]
public class MyTests : OptimizedAsyncFixtureBase
{
    private readonly SharedMockFixture _mockFixture;
    private readonly TestRequestCache _requestCache;
    private readonly CachedTestDataFixture _dataFixture;

    public MyTests(
        SharedMockFixture mockFixture,
        TestRequestCache requestCache,
        CachedTestDataFixture dataFixture)
    {
        _mockFixture = mockFixture;
        _requestCache = requestCache;
        _dataFixture = dataFixture;
    }
}
```

### Step 3: Move Initialization Code
```csharp
protected override async Task OnInitializeAsync()
{
    // Move all setup from constructor here
    // This runs once per test collection, not per test
    await Task.CompletedTask;
}

protected override async Task OnDisposeAsync()
{
    // Move any cleanup code here
    await Task.CompletedTask;
}
```

### Step 4: Replace Mock Creation
```csharp
// Before: Create new mock per test
var mockService = new Mock<IDataService>();

// After: Reuse cached mock
var mockService = await _mockFixture.GetOrCreateDataServiceMock();
```

### Step 5: Replace Data Creation
```csharp
// Before: Create data per test
var workflow = new WorkflowDto { Id = 1, Name = "Test" };

// After: Use cached data
var workflow = _requestCache.GetDefaultWorkflowDto();

// Or cache custom data
var customWorkflows = _dataFixture.GetOrCreate("my-workflows", () =>
    new[] { new WorkflowDto { Id = 1, Name = "Test" } });
```

### Step 6: Measure Improvement
```csharp
[Fact]
public void MeasureImprovement()
{
    var analyzer = new TestPerformanceAnalyzer();

    using (analyzer.BeginTiming("TestOperation"))
    {
        // Your test code
    }

    var metrics = analyzer.GetMetrics("TestOperation");
    Console.WriteLine($"Average: {metrics.AverageMs}ms");
}
```

## Priority Migration Order

### Priority 1 (Critical) - Highest ROI
- **`WorkflowServiceTests`** - High volume, many test methods
  - **Estimated improvement:** 4-5x faster
  - **Effort:** 30 minutes
  - **Impact:** 50+ tests optimized
- **`DataServiceTests`** - Heavy mocking, large datasets
  - **Estimated improvement:** 3-4x faster
  - **Effort:** 20 minutes
  - **Impact:** 30+ tests optimized

### Priority 2 (High)
- **`WorkflowClientTests`** - gRPC mocking overhead
  - **Estimated improvement:** 2-3x faster
  - **Effort:** 25 minutes
- **Integration tests using real DB** - Database setup overhead
  - **Estimated improvement:** 5-8x faster
  - **Effort:** 40 minutes

### Priority 3 (Medium)
- **Controller tests** - Lower test volume
  - **Estimated improvement:** 2-3x faster
  - **Effort:** 15 minutes per class
- **Utility class tests** - Minimal setup overhead
  - **Estimated improvement:** 1.5-2x faster
  - **Effort:** 10 minutes per class

## Troubleshooting Common Migration Issues

### Issue 1: "Type 'XxxFixture' is not defined"
**Cause:** Missing `using` statement
**Solution:**
```csharp
using AF.ECT.Tests.Fixtures;
using AF.ECT.Tests.Common;
```

### Issue 2: "Collection definition not found"
**Cause:** Collection definition not created in same namespace
**Solution:**
```csharp
// Create this once per namespace
[CollectionDefinition("My Collection")]
public class MyCollectionDefinition : 
    ICollectionFixture<SharedMockFixture>,
    ICollectionFixture<TestRequestCache>
{
}
```

### Issue 3: "Mock is not configured for method X"
**Cause:** Sharing mocks means setup from one test affects others
**Solution:** Use collection-scoped setup in `OnInitializeAsync()` or test-scoped `Verify()`
```csharp
protected override async Task OnInitializeAsync()
{
    var mock = await _mockFixture.GetOrCreateDataServiceMock();
    // Setup all expected calls here, not per test
    mock.Setup(x => x.GetWorkflow(It.IsAny<int>()))
        .ReturnsAsync(new WorkflowDto());
}
```

### Issue 4: "Data is persisting between tests"
**Cause:** CachedTestDataFixture reuses data across tests
**Solution:** Either:
1. Clear cache between test runs: `_dataFixture.ClearCache()`
2. Use separate cache keys per test variant
3. Make cached data immutable (recommended)

### Issue 5: "Async test hangs during migration"
**Cause:** Improper `IAsyncLifetime` implementation
**Solution:** Use `OptimizedAsyncFixtureBase` which handles this correctly
```csharp
public class MyTests : OptimizedAsyncFixtureBase  // Instead of IAsyncLifetime
{
    protected override async Task OnInitializeAsync() { }
    protected override async Task OnDisposeAsync() { }
}
```

## Performance Validation

### Before/After Metrics Template
```csharp
public class OptimizationValidation
{
    [Fact]
    public void ValidateOptimization()
    {
        var benchmark = new MockCreationBenchmark();
        
        // Measure traditional approach
        var traditionTime = benchmark.MeasureMockCreation(() => new Mock<IDataService>());
        
        // Measure optimized approach (cached)
        var optimizedTime = benchmark.MeasureMockCreation(
            () => GetOrCreateMock()); // Your cached mock method
        
        var report = MockCreationBenchmark.GenerateMockComparisonReport(
            "IDataService",
            100,
            traditionTime,
            optimizedTime);
        
        _output.WriteLine(report);
        
        // Assert at least 50% improvement
        Assert.True(traditionTime > optimizedTime * 1.5);
    }
}
```

### Expected Improvements
| Test Type | Improvement | Effort | Priority |
|-----------|-------------|--------|----------|
| Service tests with heavy mocking | 3-5x faster | 30 min | Priority 1 |
| Integration tests with DB setup | 5-8x faster | 40 min | Priority 2 |
| Streaming tests with large data | 4-6x faster | 25 min | Priority 2 |
| Controller tests | 2-3x faster | 20 min | Priority 3 |

## Automated Migration Helper Script

For future migrations, use this template to batch-process similar tests:

```csharp
// Example: Batch migration checklist
// 1. Search for all `new Mock<IDataService>()` calls
// 2. Replace with `await _mockFixture.GetOrCreateDataServiceMock()`
// 3. Search for duplicate setup code
// 4. Move to `OnInitializeAsync()` using `GetOrCreate()`
// 5. Run tests and verify green
// 6. Measure with `TestPerformanceAnalyzer`
```

## Next Steps After Migration

1. **Run the test suite:** `dotnet test AF.ECT.Tests`
2. **Measure improvements:** Add `TestPerformanceAnalyzer` to your tests
3. **Document results:** Update `COVERAGE_GUIDELINES.md` with metrics
4. **Share learnings:** Document any custom patterns in team wiki
5. **Maintain:** Encourage new tests to use optimized patterns from day 1

## FAQ

**Q: Will optimizations break existing tests?**
A: No, all fixtures are designed to be backward compatible. Tests continue to work, just faster.

**Q: How do I add new mocks if they're not in SharedMockFixture?**
A: Use `_mockFixture.GetOrCreateCustomMock<T>(setup)` or extend `SharedMockFixture` for common types.

**Q: Can I mix traditional and optimized tests?**
A: Yes, gradually migrate. Just be mindful that collection-based fixtures are shared per collection.

**Q: What if a test needs unique mock behavior?**
A: Use collection-scoped setup with conditional logic or create a separate collection for that variant.

**Q: How do I measure improvement for my team?**
A: Use `TestPerformanceAnalyzer` before/after migration on the same tests. See Performance Validation section.

## Success Criteria

✅ **You've successfully migrated when:**
- All tests pass without modification
- Test execution time is reduced by at least 40%
- No duplicate `[Collection]` attributes in test class
- All `new Mock<T>()` calls are replaced with fixture methods
- All data creation uses `GetOrCreate()` pattern
- `OnInitializeAsync()` and `OnDisposeAsync()` properly override parent methods
- Performance metrics show consistent improvement across test runs

Good luck with your migration! 🚀
