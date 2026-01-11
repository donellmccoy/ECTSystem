# gRPC Streaming Patterns and Best Practices

## Overview

This document provides comprehensive guidance on implementing gRPC streaming in the ECTSystem with focus on resilience patterns, cancellation handling, audit logging, and error handling. The patterns and practices outlined here are based on production-ready implementations in `WorkflowClient`.

## Table of Contents

1. [Streaming Fundamentals](#streaming-fundamentals)
2. [Resilience Patterns](#resilience-patterns)
3. [Cancellation and Timeout Handling](#cancellation-and-timeout-handling)
4. [Audit Logging for Streaming](#audit-logging-for-streaming)
5. [Backpressure and Buffering](#backpressure-and-buffering)
6. [Error Handling](#error-handling)
7. [Memory and Resource Management](#memory-and-resource-management)
8. [Code Examples](#code-examples)

---

## Streaming Fundamentals

### Server Streaming Basics

Server streaming allows the gRPC server to send multiple messages to the client in response to a single request. This is ideal for large datasets, real-time updates, and data that flows naturally as a sequence.

**When to use server streaming:**
- Returning large result sets (users, cases, findings, etc.)
- Real-time data feeds or event streams
- Situations where client can process items as they arrive
- Reducing memory footprint on server (items returned one at a time)

### Async Enumerables and Iterators

In ECTSystem, streaming is exposed through `IAsyncEnumerable<T>` patterns:

```csharp
// Server-side: Define streaming method
async IAsyncEnumerable<User> GetUsersOnlineStream(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    foreach (var user in users)
    {
        ct.ThrowIfCancellationRequested();
        yield return user;
    }
}

// Client-side: Consume streaming data
await foreach (var user in client.GetUsersOnlineStreamAsync(cancellationToken))
{
    // Process each user as it arrives
    ProcessUser(user);
}
```

**Key Points:**
- Use `[EnumeratorCancellation]` attribute to allow cancellation of async enumerables
- Always call `ThrowIfCancellationRequested()` at strategic points
- `await foreach` is syntactic sugar for `IAsyncEnumerable` consumption
- Each iteration fetches the next chunk from the stream

---

## Resilience Patterns

### Retry Policy Implementation

The `WorkflowClient` implements exponential backoff retry policy for transient failures:

```csharp
// Configuration via WorkflowClientOptions
public class WorkflowClientOptions
{
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(50, 5000)]
    public int InitialRetryDelayMs { get; set; } = 100;

    [Range(500, 10000)]
    public int MaxRetryDelayMs { get; set; } = 1000;

    [Range(10, 300)]
    public int RequestTimeoutSeconds { get; set; } = 30;
}
```

### Building Resilience Policies

The `WorkflowClient` combines multiple Polly policies:

```csharp
// Retry policy with exponential backoff
_retryPolicy = Policy.Handle<Grpc.Core.RpcException>(ex =>
        ex.StatusCode == Grpc.Core.StatusCode.Unavailable ||
        ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded ||
        ex.StatusCode == Grpc.Core.StatusCode.Internal)
    .WaitAndRetryAsync(_options.MaxRetryAttempts,
        attempt => TimeSpan.FromMilliseconds(
            Math.Min(
                _options.InitialRetryDelayMs * Math.Pow(2, attempt),
                _options.MaxRetryDelayMs)),
        onRetry: (exception, timeSpan, retryCount, context) =>
        {
            _logger?.LogWarning(exception, 
                "gRPC call failed, retrying in {Delay}ms (attempt {Attempt}/{MaxAttempts})",
                timeSpan.TotalMilliseconds, retryCount, _options.MaxRetryAttempts);
        });

// Timeout policy
_timeoutPolicy = Policy.TimeoutAsync(
    TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

// Combine all policies
_resiliencePolicy = Policy.WrapAsync(_retryPolicy, _timeoutPolicy);
```

### Applying Resilience to Streaming Calls

For unary gRPC calls, apply the resilience policy directly:

```csharp
public async Task<GetUsersResponse> GetUsersAsync(GetUsersRequest request, 
    CancellationToken ct = default)
{
    return await _resiliencePolicy.ExecuteAsync(async () =>
    {
        return await _client.GetUsersAsync(request, cancellationToken: ct);
    });
}
```

**Important Note for Streaming:**
Polly resilience policies are applied at the **method invocation level**, not within the stream itself. Once a streaming call is initiated, individual item failures should be handled with application-level logic or stream termination.

### Circuit Breaker Pattern

While not implemented in the current `WorkflowClient`, circuit breakers prevent cascading failures:

```csharp
// Circuit breaker opens after 5 failures for 30 seconds
var circuitBreakerPolicy = Policy.Handle<RpcException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30));
```

**When to use:**
- Protect downstream services from overload
- Prevent repeated calls to unavailable services
- Allow time for service recovery

---

## Cancellation and Timeout Handling

### Cancellation Token Propagation

Always propagate `CancellationToken` through the entire call chain:

```csharp
// Server-side: Accept and monitor cancellation token
async IAsyncEnumerable<Item> GetItemsStream(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    foreach (var item in GetItemsFromDatabase())
    {
        // Critical: Check cancellation at each iteration
        ct.ThrowIfCancellationRequested();
        
        yield return item;
    }
}

// Client-side: Pass cancellation token
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    await foreach (var item in client.GetItemsStreamAsync(cts.Token))
    {
        ProcessItem(item);
    }
}
catch (OperationCanceledException)
{
    // Handle cancellation gracefully
}
finally
{
    cts.Dispose();
}
```

### Timeout Implementation

Timeouts prevent indefinite hangs and resource exhaustion:

```csharp
// Automatic timeout via Polly
var timeoutPolicy = Policy.TimeoutAsync(
    TimeSpan.FromSeconds(30));

// Or explicit timeout with CancellationTokenSource
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    await foreach (var item in stream.GetItemsStreamAsync(cts.Token))
    {
        // Process items
    }
}
catch (OperationCanceledException)
{
    // Timeout occurred
    _logger.LogWarning("Streaming operation timed out");
}
```

### Best Practices for Cancellation

1. **Always check cancellation tokens** at logical boundaries
2. **Use CancellationTokenSource** for explicit timeout management
3. **Combine with timeouts** for defensive programming
4. **Log cancellation events** for observability
5. **Clean up resources** in finally blocks

---

## Audit Logging for Streaming

### Enabling Audit.NET for Streaming

Configure Audit.NET in `Program.cs`:

```csharp
// Setup Audit.NET
Audit.Core.Configuration.Setup()
    .UseSqlServer(config => config
        .ConnectionString(connectionString)
        .Schema("dbo")
        .TableName("AuditLogs"))
    .WithAction(action => action
        .OnCreatingScope((scope, key, value) =>
        {
            // Add custom scope context
        }));
```

### Logging Streaming Operations

The `WorkflowClient.AuditLogging.cs` partial class provides streaming audit logging:

```csharp
/// <summary>
/// Logs a streaming operation with performance metrics and correlation ID.
/// </summary>
public void LogStreamingAuditEvent(
    string methodName,
    string correlationId,
    DateTime startTime,
    int itemCount,
    TimeSpan duration,
    bool success,
    string? errorMessage,
    Dictionary<string, object>? additionalData = null)
{
    var itemsPerSecond = duration.TotalSeconds > 0 
        ? itemCount / duration.TotalSeconds 
        : 0;

    using (var auditScope = AuditScope.Create("gRPC:Streaming", () =>
    {
        return new
        {
            MethodName = methodName,
            CorrelationId = correlationId,
            ItemCount = itemCount,
            ItemsPerSecond = Math.Round(itemsPerSecond, 2),
            Duration = duration.TotalMilliseconds,
            Success = success,
            ErrorMessage = errorMessage,
            AdditionalData = additionalData
        };
    }))
    {
        auditScope.SetCustomField("CorrelationId", correlationId);
        auditScope.SetCustomField("Success", success);
        auditScope.SetCustomField("ItemCount", itemCount);
        auditScope.SetCustomField("ItemsPerSecond", itemsPerSecond);
    }

    _logger?.LogInformation(
        "Streaming operation {MethodName} completed: {ItemCount} items in {Duration}ms " +
        "(Correlation: {CorrelationId})",
        methodName, itemCount, duration.TotalMilliseconds, correlationId);
}
```

### Using Correlation IDs

Correlation IDs enable end-to-end traceability:

```csharp
// Generate correlation ID at request entry point
var correlationId = Guid.NewGuid().ToString();

// Add to request context
var context = new Dictionary<string, object>
{
    { "CorrelationId", correlationId },
    { "UserId", currentUserId },
    { "Timestamp", DateTime.UtcNow }
};

// Log streaming completion with correlation ID
LogStreamingAuditEvent(
    methodName: "GetUsersOnlineStream",
    correlationId: correlationId,
    startTime: startTime,
    itemCount: 500,
    duration: sw.Elapsed,
    success: true,
    errorMessage: null,
    additionalData: context);
```

### Audit Trail Storage

Audit events are stored in the `AuditLogs` table with:
- **EventType**: "gRPC:Streaming" (structured as "gRPC:{Method}")
- **AuditData**: JSON containing method name, correlation ID, item count, duration
- **CreatedDate**: Timestamp of event
- **Success**: Boolean indicating operation success
- **Duration**: Operation time in milliseconds

---

## Backpressure and Buffering

### Understanding Backpressure

Backpressure occurs when the producer (server) sends data faster than the consumer (client) can process it. This can lead to memory exhaustion.

```csharp
// Problem: Fast producer, slow consumer
public async Task SlowConsumerProblem()
{
    // Server streams 10,000 items/sec
    // Client can only process 100 items/sec
    // Result: Memory usage grows unbounded
    
    var buffer = new List<Item>();
    await foreach (var item in fastProducerStream)
    {
        buffer.Add(item); // Buffer grows to 100,000+ items!
        await SlowProcessing(item); // Only processes 1/100th rate
    }
}
```

### Buffering Strategies

**Strategy 1: Process Immediately (Preferred)**

```csharp
// No buffering - process as items arrive
await foreach (var item in stream)
{
    // Automatically applies backpressure
    await ProcessItem(item);
    
    // Slow processing naturally throttles producer
}
```

**Strategy 2: Bounded Channel**

```csharp
// Bounded buffer for controlled processing
var channel = Channel.CreateBounded<Item>(
    new BoundedChannelOptions(1000) // Max 1000 items
    {
        FullMode = BoundedChannelFullMode.Wait
    });

// Producer writes to channel
_ = Task.Run(async () =>
{
    await foreach (var item in stream)
    {
        await channel.Writer.WriteAsync(item);
    }
    channel.Writer.Complete();
});

// Consumer reads with backpressure
await foreach (var item in channel.Reader.ReadAllAsync())
{
    await ProcessItem(item);
}
```

**Strategy 3: Batch Processing**

```csharp
// Process items in batches
var batch = new List<Item>();
const int batchSize = 100;

await foreach (var item in stream)
{
    batch.Add(item);
    
    if (batch.Count >= batchSize)
    {
        await ProcessBatch(batch);
        batch.Clear();
    }
}

// Process remaining items
if (batch.Count > 0)
{
    await ProcessBatch(batch);
}
```

### Monitoring Backpressure

```csharp
// Track consumption rate
var stopwatch = Stopwatch.StartNew();
var itemCount = 0;

await foreach (var item in stream)
{
    itemCount++;
    
    if (itemCount % 1000 == 0)
    {
        var itemsPerSecond = itemCount / stopwatch.Elapsed.TotalSeconds;
        _logger.LogInformation(
            "Consuming {ItemsPerSecond} items/sec",
            Math.Round(itemsPerSecond, 2));
    }
    
    await ProcessItem(item);
}
```

---

## Error Handling

### gRPC Exception Handling

gRPC failures manifest as `RpcException`:

```csharp
try
{
    await foreach (var item in stream)
    {
        ProcessItem(item);
    }
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
{
    // Server unreachable - may retry
    _logger.LogWarning("Server unavailable: {Message}", ex.Message);
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
{
    // Timeout - don't retry without backoff
    _logger.LogWarning("Deadline exceeded");
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.Internal)
{
    // Server error - log and potentially retry
    _logger.LogError(ex, "Server error during streaming");
}
catch (OperationCanceledException)
{
    // Cancellation requested
    _logger.LogInformation("Streaming cancelled");
}
catch (Exception ex)
{
    // Unexpected error - log and fail
    _logger.LogError(ex, "Unexpected error in streaming");
    throw;
}
```

### Status Code Meanings

| Status Code | Meaning | Action |
|---|---|---|
| `OK` | Success | No action |
| `Unavailable` | Server down/unreachable | Retry with backoff |
| `DeadlineExceeded` | Timeout | Don't retry, increase timeout |
| `Internal` | Server error | Retry with backoff |
| `InvalidArgument` | Bad request | Fix and resubmit |
| `PermissionDenied` | Auth failure | Don't retry |
| `NotFound` | Resource missing | Don't retry |

### Partial Failure Handling

When streaming, handle mid-stream failures gracefully:

```csharp
// Accumulate successfully processed items
var processedItems = new List<Item>();
var failedAt = -1;

try
{
    int index = 0;
    await foreach (var item in stream)
    {
        try
        {
            ProcessItem(item);
            processedItems.Add(item);
        }
        catch (Exception ex)
        {
            failedAt = index;
            _logger.LogError(ex, "Failed processing item at index {Index}", index);
            throw; // Stop streaming on error
        }
        index++;
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Streaming failed after {ProcessedCount} items", 
        processedItems.Count);
    
    // Potentially retry, depending on error type
    if (failedAt >= 0)
    {
        // Could implement resume logic here
    }
}
```

---

## Memory and Resource Management

### Channel Reuse

gRPC channels are expensive to create - reuse them:

```csharp
// ✅ Good: Single channel, multiple calls
var channel = GrpcChannel.ForAddress("https://localhost:5000");
var client1 = new Service1Client(channel);
var client2 = new Service2Client(channel);

// Later...
await client1.StreamMethod1Async(request);
await client2.StreamMethod2Async(request);

// Dispose once
channel.Dispose();

// ❌ Bad: New channel per call
for (int i = 0; i < 100; i++)
{
    var channel = GrpcChannel.ForAddress("https://localhost:5000");
    var client = new ServiceClient(channel);
    await client.StreamAsync(request);
    channel.Dispose(); // Expensive!
}
```

### Async Enumerable Lifecycle

```csharp
// Async enumerables are lazy - evaluation doesn't start until enumeration
var stream = client.GetItemsStreamAsync(ct); // No work yet

// Work begins when you iterate
await foreach (var item in stream) // Now items are fetched
{
    ProcessItem(item);
} // Stream ends here
```

### Resource Cleanup

```csharp
public async Task SafeStreamingWithCleanup()
{
    var channel = GrpcChannel.ForAddress("https://localhost:5000");
    var client = new ServiceClient(channel);
    var cts = new CancellationTokenSource();
    
    try
    {
        await foreach (var item in client.StreamAsync(request, cancellationToken: cts.Token))
        {
            ProcessItem(item);
        }
    }
    finally
    {
        cts.Dispose();
        channel.Dispose();
    }
}
```

---

## Code Examples

### Example 1: Complete Streaming Implementation

```csharp
public class UserService
{
    private readonly WorkflowClient _workflowClient;
    private readonly ILogger<UserService> _logger;

    public UserService(WorkflowClient workflowClient, ILogger<UserService> logger)
    {
        _workflowClient = workflowClient;
        _logger = logger;
    }

    public async Task ProcessAllUsersAsync(CancellationToken ct = default)
    {
        var correlationId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var processedCount = 0;
        var sw = Stopwatch.StartNew();

        try
        {
            await foreach (var user in _workflowClient
                .GetUsersOnlineStreamAsync(ct))
            {
                try
                {
                    // Process user
                    await ProcessUserAsync(user, correlationId);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error processing user {UserId}", user.UserId);
                    // Continue processing remaining users
                }
            }

            // Log successful completion
            _workflowClient.LogStreamingAuditEvent(
                methodName: "GetUsersOnlineStream",
                correlationId: correlationId,
                startTime: startTime,
                itemCount: processedCount,
                duration: sw.Elapsed,
                success: true,
                errorMessage: null,
                additionalData: new Dictionary<string, object>
                {
                    { "UserId", "SystemUser" },
                    { "OperationType", "BulkProcess" }
                });
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("User processing cancelled after {Count} users", 
                processedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error processing users");
            
            // Log failure
            _workflowClient.LogStreamingAuditEvent(
                methodName: "GetUsersOnlineStream",
                correlationId: correlationId,
                startTime: startTime,
                itemCount: processedCount,
                duration: sw.Elapsed,
                success: false,
                errorMessage: ex.Message);

            throw;
        }
    }

    private async Task ProcessUserAsync(User user, string correlationId)
    {
        // Application logic here
        await Task.CompletedTask;
    }
}
```

### Example 2: Streaming with Backpressure Control

```csharp
public async Task StreamWithBackpressureAsync(CancellationToken ct)
{
    var channel = Channel.CreateBounded<Item>(
        new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    // Producer task
    var producerTask = Task.Run(async () =>
    {
        try
        {
            await foreach (var item in _client.GetItemsStreamAsync(ct))
            {
                await channel.Writer.WriteAsync(item, ct);
            }
        }
        finally
        {
            channel.Writer.Complete();
        }
    });

    // Consumer task
    var consumerTask = Task.Run(async () =>
    {
        await foreach (var item in channel.Reader.ReadAllAsync(ct))
        {
            await ProcessItemWithDelayAsync(item);
        }
    });

    // Wait for both to complete
    await Task.WhenAll(producerTask, consumerTask);
}

private async Task ProcessItemWithDelayAsync(Item item)
{
    // Simulate slow processing
    await Task.Delay(100);
    // Process item
}
```

### Example 3: Streaming with Retry

```csharp
public async Task StreamWithRetryAsync(CancellationToken ct)
{
    var maxRetries = 3;
    var retryDelay = TimeSpan.FromSeconds(1);
    
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            await foreach (var item in _client.GetItemsStreamAsync(ct))
            {
                ProcessItem(item);
            }
            return; // Success
        }
        catch (RpcException ex) when (
            ex.StatusCode == StatusCode.Unavailable ||
            ex.StatusCode == StatusCode.Internal)
        {
            _logger.LogWarning(
                "Streaming failed (attempt {Attempt}/{Max}): {Message}",
                attempt + 1, maxRetries, ex.Message);

            if (attempt < maxRetries - 1)
            {
                await Task.Delay(retryDelay, ct);
                retryDelay = retryDelay.Add(TimeSpan.FromSeconds(1)); // Backoff
            }
        }
    }

    throw new InvalidOperationException("Streaming failed after max retries");
}
```

---

## Summary of Best Practices

| Practice | Benefit |
|---|---|
| Always propagate `CancellationToken` | Enables graceful shutdown and timeout control |
| Process items immediately | Prevents backpressure and memory issues |
| Log with correlation IDs | Enables end-to-end traceability |
| Apply resilience policies | Handles transient failures automatically |
| Check cancellation at boundaries | Responsive to shutdown requests |
| Use bounded channels | Prevents unbounded buffering |
| Log errors and completions | Enables operational visibility |
| Reuse channels | Reduces resource overhead |
| Handle status codes specifically | Enables targeted error recovery |
| Clean up resources in finally | Prevents resource leaks |

---

## References

- [gRPC in .NET Documentation](https://learn.microsoft.com/en-us/dotnet/api/grpc)
- [Async Enumerables in C#](https://learn.microsoft.com/en-us/archive/msdn-magazine/2019-november/csharp-iterating-with-async-enumerables-in-csharp-8)
- [Polly Resilience Patterns](https://github.com/App-vNext/Polly)
- [Audit.NET Documentation](https://github.com/thepirat000/Audit.NET)
- [gRPC RPC Status Codes](https://grpc.io/docs/guides/status-codes/)
