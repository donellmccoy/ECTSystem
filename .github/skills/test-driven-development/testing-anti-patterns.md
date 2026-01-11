# Testing Anti-Patterns

**Load this reference when:** writing or changing tests, adding mocks, or tempted to add test-only methods to production code.

## Overview

Tests must verify real behavior, not mock behavior. Mocks are a means to isolate, not the thing being tested.

**Core principle:** Test what the code does, not what the mocks do.

**Following strict TDD prevents these anti-patterns.**

## The Iron Laws

```
1. NEVER test mock behavior
2. NEVER add test-only methods to production classes
3. NEVER mock without understanding dependencies
```

## Anti-Pattern 1: Testing Mock Behavior

**The violation:**
```csharp
// ❌ BAD: Testing that the mock exists
[Fact]
public void Render_Sidebar_VerifiesMockExists()
{
    var mockSidebar = new Mock<ISidebar>();
    var component = new PageComponent(mockSidebar.Object);
    
    // Only verifies mock was injected, not real behavior
    mockSidebar.Verify(s => s.Render(), Times.Once);
}
```

**Why this is wrong:**
- You're verifying the mock works, not that the component works
- Test passes when mock is present, fails when it's not
- Tells you nothing about real behavior

**Your AI partner's question:** "Are we testing the behavior of a mock?"

**The fix:**
```csharp
// ✅ GOOD: Test real component behavior or don't mock it
[Fact]
public void PageComponent_WithSidebar_RendersSidebarContent()
{
    // Use real sidebar component - no mock needed
    var component = new PageComponent(new SidebarComponent());
    
    var result = component.Render();
    Assert.Contains("navigation", result);
}

// OR if sidebar must be mocked for speed:
// Don't assert on the mock - test PageComponent's behavior with sidebar present
[Fact]
public async Task PageComponent_LoadsDataBeforeSidebarRenders()
{
    var mockSidebar = new Mock<ISidebar>();
    var component = new PageComponent(mockSidebar.Object);
    
    // Test real behavior: data loading happens before sidebar renders
    var result = await component.InitializeAsync();
    
    Assert.True(component.DataLoaded);  // Test real logic, not mock call
}
```

**C# .NET Example (ECTSystem):**

```csharp
// ❌ BAD: Testing mock behavior not real EF Core query
[Fact]
public async Task GetUsersByStatus_ReturnsUsers()
{
    var mockDataService = new Mock<IWorkflowDataService>();
    mockDataService.Setup(s => s.GetUsersByStatusAsync(It.IsAny<string>()))
        .ReturnsAsync(new[] { new User { Id = "1", Status = "Active" } });
    
    var service = new UserServiceImpl(mockDataService.Object);
    var result = await service.GetUsersByStatus("Active");
    
    Assert.NotEmpty(result);  // Testing mock returns data, not real query works
}

// ✅ GOOD: Test real EF Core query with InMemoryDatabase
[Fact]
public async Task GetUsersByStatus_ReturnsActiveUsers()
{
    // Use real EF Core with InMemory database
    var options = new DbContextOptionsBuilder<EctDbContext>()
        .UseInMemoryDatabase("test-db")
        .Options;
    
    using (var context = new EctDbContext(options))
    {
        // Set up real test data
        context.Users.AddRange(
            new User { Id = "1", Name = "John", Status = "Active" },
            new User { Id = "2", Name = "Jane", Status = "Inactive" },
            new User { Id = "3", Name = "Bob", Status = "Active" }
        );
        await context.SaveChangesAsync();

        var dataService = new UserDataService(context);
        var result = await dataService.GetUsersByStatusAsync("Active");

        // Test real behavior - query filters correctly
        Assert.Equal(2, result.Count);
        Assert.All(result, u => Assert.Equal("Active", u.Status));
    }
}
```

### Gate Function

```
BEFORE asserting on any mock element:
  Ask: "Am I testing real component behavior or just mock existence?"

  IF testing mock existence:
    STOP - Delete the assertion or unmock the component

  Test real behavior instead
```

## Anti-Pattern 2: Test-Only Methods in Production

**The violation:**
```csharp
// ❌ BAD: Reset() only used in tests
public class WorkflowService
{
    public void Reset() // Don't belong in production!
    {
        _cache.Clear();
        _auditLog.Clear();
        // Only called by tests
    }
}

// In tests
[Fact]
public async Task TestSomething()
{
    var service = new WorkflowService();
    // ... test code ...
    service.Reset(); // Bad - confuses test cleanup with production API
}
```

**Why this is wrong:**
- Production class polluted with test-only code
- Dangerous if accidentally called in production
- Violates YAGNI and separation of concerns
- Confuses object lifecycle with entity lifecycle

**The fix:**
```csharp
// ✅ GOOD: Use xUnit fixtures with IAsyncLifetime for cleanup
// WorkflowService has no Reset() method - it's stateless in production

public class WorkflowServiceFixture : IAsyncLifetime
{
    private readonly EctDbContext _context;
    
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<EctDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new EctDbContext(options);
    }
    
    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }
    
    public WorkflowService CreateService() => new WorkflowService(_context);
}

[Fact]
public async Task TestSomething()
{
    await using var fixture = new WorkflowServiceFixture();
    var service = fixture.CreateService();
    
    // ... test code ...
    // Cleanup happens automatically via DisposeAsync
}
```

### Gate Function

```
BEFORE adding any method to production class:
  Ask: "Is this only used by tests?"

  IF yes:
    STOP - Don't add it
    Put it in test utilities instead

  Ask: "Does this class own this resource's lifecycle?"

  IF no:
    STOP - Wrong class for this method
```

## Anti-Pattern 3: Mocking Without Understanding

**The violation:**
```csharp
// ❌ BAD: Mock breaks test logic
[Fact]
public async Task GetWorkflow_WithDuplicateApproval_ThrowsRpcException()
{
    // Mock prevents database write that test depends on!
    var mockDataService = new Mock<IWorkflowDataService>();
    mockDataService.Setup(s => s.ApproveWorkflowAsync(It.IsAny<string>()))
        .ReturnsAsync(new WorkflowResponse()); // Side effect: updating audit log is skipped
    
    var service = new WorkflowServiceImpl(mockDataService.Object);
    
    await service.ApproveWorkflow(new ApprovalRequest { WorkflowId = "WF-1" }, _context);
    await service.ApproveWorkflow(new ApprovalRequest { WorkflowId = "WF-1" }, _context);  
    // Should throw RpcException for duplicate - but won't because mock skips validation!
}
```

**Why this is wrong:**
- Mocked method had side effect test depends on (audit logging and duplicate checking)
- Over-mocking to "isolate" breaks actual behavior the test needs
- Test passes for wrong reason

**C# .NET Example (ECTSystem):**

```csharp
// ✅ GOOD: Mock at correct level - use real EF Core with InMemory
[Fact]
public async Task ApproveWorkflow_WithDuplicateApproval_ThrowsRpcException()
{
    // Use real EF Core - only mock external services (email, file storage)
    var options = new DbContextOptionsBuilder<EctDbContext>()
        .UseInMemoryDatabase("test-db")
        .Options;
    
    using var context = new EctDbContext(options);
    context.Workflows.Add(new Workflow { Id = "WF-1", Status = "Pending" });
    await context.SaveChangesAsync();
    
    var mockEmailService = new Mock<IEmailService>(); // Only mock slow external service
    var service = new WorkflowServiceImpl(context, mockEmailService.Object);
    
    // First approval - succeeds
    await service.ApproveWorkflow(new ApprovalRequest { WorkflowId = "WF-1" }, _context);
    
    // Second approval - should throw duplicate error
    var ex = await Assert.ThrowsAsync<RpcException>(
        () => service.ApproveWorkflow(new ApprovalRequest { WorkflowId = "WF-1" }, _context));
    
    Assert.Equal(StatusCode.AlreadyExists, ex.Status.StatusCode);
}
```

### Gate Function

```
BEFORE mocking any method:
  STOP - Don't mock yet

  1. Ask: "What side effects does the real method have?"
  2. Ask: "Does this test depend on any of those side effects?"
  3. Ask: "Do I fully understand what this test needs?"

  IF depends on side effects:
    Mock at lower level (the actual slow/external operation)
    OR use test doubles that preserve necessary behavior
    NOT the high-level method the test depends on

  IF unsure what test depends on:
    Run test with real implementation FIRST
    Observe what actually needs to happen
    THEN add minimal mocking at the right level

  Red flags:
    - "I'll mock this to be safe"
    - "This might be slow, better mock it"
    - Mocking without understanding the dependency chain
```

## Anti-Pattern 4: Incomplete Mocks

**The violation:**
```csharp
// ❌ BAD: Partial mock - only fields you think you need
var mockResponse = new WorkflowResponse
{
    WorkflowId = "WF-123",
    Status = "Approved"
    // Missing: AuditLog, CorrelationId, Metadata that downstream code uses
};

// Later: breaks when code accesses response.Metadata.RequestId
```

**Why this is wrong:**
- **Partial mocks hide structural assumptions** - You only mocked fields you know about
- **Downstream code may depend on fields you didn't include** - Silent failures
- **Tests pass but integration fails** - Mock incomplete, real gRPC response complete
- **False confidence** - Test proves nothing about real behavior

**C# .NET Example (ECTSystem):**

**The Iron Rule:** Mock the COMPLETE data structure as it exists in reality, not just fields your immediate test uses.

```csharp
// ✅ GOOD: Complete mock with all required fields
var mockResponse = new WorkflowResponse
{
    WorkflowId = "WF-123",
    Status = "Approved",
    ApprovedBy = "user@example.com",
    ApprovedAt = DateTime.UtcNow,
    AuditLog = new AuditLog 
    { 
        EventType = "WorkflowApproved",
        CorrelationId = "corr-456",
        Timestamp = DateTime.UtcNow,
        Duration = 125
    },
    Metadata = new ResponseMetadata
    {
        RequestId = "req-789",
        Version = "1.0"
    }
    // All fields real gRPC response returns
};
```

### Gate Function

```
BEFORE creating mock responses:
  Check: "What fields does the real API response contain?"

  Actions:
    1. Examine actual API response from docs/examples
    2. Include ALL fields system might consume downstream
    3. Verify mock matches real response schema completely

  Critical:
    If you're creating a mock, you must understand the ENTIRE structure
    Partial mocks fail silently when code depends on omitted fields

  If uncertain: Include all documented fields
```

## Anti-Pattern 5: Integration Tests as Afterthought

**The violation:**
```
✅ Implementation complete
❌ No tests written
"Ready for testing"
```

**Why this is wrong:**
- Testing is part of implementation, not optional follow-up
- TDD would have caught this
- Can't claim complete without tests

**The fix:**
```
TDD cycle:
1. Write failing test
2. Implement to pass
3. Refactor
4. THEN claim complete
```

## When Mocks Become Too Complex

**Warning signs:**
- Mock setup longer than test logic
- Mocking everything to make test pass
- Mocks missing methods real components have
- Test breaks when mock changes

**your human partner's question:** "Do we need to be using a mock here?"

**Consider:** Integration tests with real components often simpler than complex mocks

## TDD Prevents These Anti-Patterns

**Why TDD helps:**
1. **Write test first** → Forces you to think about what you're actually testing
2. **Watch it fail** → Confirms test tests real behavior, not mocks
3. **Minimal implementation** → No test-only methods creep in
4. **Real dependencies** → You see what the test actually needs before mocking

**If you're testing mock behavior, you violated TDD** - you added mocks without watching test fail against real code first.

## Quick Reference

| Anti-Pattern | Fix |
|--------------|-----|
| Assert on mock elements | Test real component or unmock it |
| Test-only methods in production | Move to test utilities |
| Mock without understanding | Understand dependencies first, mock minimally |
| Incomplete mocks | Mirror real API completely |
| Tests as afterthought | TDD - tests first |
| Over-complex mocks | Consider integration tests |

## Red Flags

- Assertion checks for `*-mock` test IDs
- Methods only called in test files
- Mock setup is >50% of test
- Test fails when you remove mock
- Can't explain why mock is needed
- Mocking "just to be safe"

## The Bottom Line

**Mocks are tools to isolate, not things to test.**

If TDD reveals you're testing mock behavior, you've gone wrong.

Fix: Test real behavior or question why you're mocking at all.
