# Streaming Optimization and Integration Guide

## Database Query Optimization for Streaming

### 1. Streaming Stored Procedures Design

Optimize database queries for streaming scenarios where large result sets are returned incrementally.

```sql
-- ✅ GOOD: Streaming-optimized stored procedure with pagination
CREATE PROCEDURE sp_GetUsersOnlineStream
    @PageSize INT = 1000
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Offset INT = 0;
    
    -- Use keyset-based pagination for efficient large result sets
    -- Returns items in batches to avoid large result set accumulation
    WHILE 1 = 1
    BEGIN
        SELECT TOP(@PageSize)
            UserId,
            UserName,
            LastActive,
            Status
        FROM core.User
        WHERE IsActive = 1
        ORDER BY UserId
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;
        
        IF @@ROWCOUNT < @PageSize
            BREAK;
            
        SET @Offset = @Offset + @PageSize;
    END
END
GO

-- ❌ POOR: Non-optimized query returning entire result set
-- Causes memory issues with large datasets
SELECT UserId, UserName, LastActive, Status
FROM core.User
WHERE IsActive = 1;
```

### 2. Index Strategy for Streaming

Create covering indexes to minimize disk I/O for streaming queries.

```sql
-- Create index that supports streaming queries
CREATE NONCLUSTERED INDEX IX_User_IsActive_Streaming
ON core.User (IsActive, UserId)
INCLUDE (UserName, LastActive, Status)
WHERE IsActive = 1;

-- This index:
-- - Filters on IsActive (WHERE clause)
-- - Sorts by UserId (ORDER BY)
-- - Includes all required columns (INCLUDE)
-- - Is filtered (sparse index, only active users)
```

### 3. Query Execution Patterns

```csharp
// ✅ GOOD: Stream from database efficiently
public async IAsyncEnumerable<UserOnlineItem> GetUsersOnlineStream()
{
    // Use ExecuteReader for streaming (not ToList)
    using var call = _client.GetUsersOnlineStream(new EmptyRequest());
    
    while (await call.ResponseStream.MoveNext(CancellationToken.None))
    {
        yield return call.ResponseStream.Current;
    }
}

// ❌ POOR: Loads entire result set into memory
public async Task<List<UserOnlineItem>> GetUsersOnlineAsync()
{
    var response = await _client.GetUsersAsync(new EmptyRequest());
    return response.Items.ToList(); // All items loaded at once
}
```

### 4. Connection Pooling Configuration

```csharp
// Program.cs - Configure connection pooling for optimal streaming

var connectionString = "Server=localhost;Database=ECT;Min Pool Size=5;Max Pool Size=100;Pooling=true;";

services.AddDbContext<ECTContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Enable MARS (Multiple Active Result Sets) for concurrent streaming
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
        sqlOptions.CommandTimeout(30); // 30-second timeout for streaming queries
    }));
```

### 5. Query Monitoring

Monitor query performance for streaming operations:

```csharp
// Log slow streaming queries
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.LogTo(
        logger => Console.WriteLine,
        new[] { RelationalEventId.CommandExecuted },
        LogLevel.Information);
    
    optionsBuilder.EnableSensitiveDataLogging();
}
```

---

## Streaming Integration with Blazor WebClient

### 1. Setting Up gRPC-Web in Blazor

The Blazor WASM client can stream data from the server using gRPC-Web.

```csharp
// Program.cs - Blazor client configuration

builder.Services
    .AddGrpcClient<WorkflowService.WorkflowServiceClient>(o =>
    {
        o.Address = new Uri(builder.HostEnvironment.BaseAddress);
    })
    .ConfigureChannel(o =>
    {
        // Enable gRPC-Web for browser compatibility
        o.HttpHandler = new GrpcWebHandler(new HttpClientHandler());
    });
```

### 2. Consuming Streams in Blazor Components

```razor
@* StreamingDataComponent.razor - Real-time data streaming *@
@implements IAsyncDisposable
@using AF.ECT.Shared.Services
@inject WorkflowClient WorkflowClient

<div class="streaming-container">
    <h3>Live User Count: @UsersOnlineCount</h3>
    
    @if (Users.Count > 0)
    {
        <table class="table">
            <thead>
                <tr>
                    <th>User ID</th>
                    <th>Name</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var user in Users)
                {
                    <tr>
                        <td>@user.UserId</td>
                        <td>@user.UserName</td>
                        <td>@user.Status</td>
                    </tr>
                }
            </tbody>
        </table>
    }
    else
    {
        <p>Loading users...</p>
    }
    
    @if (IsStreaming)
    {
        <button @onclick="StopStreaming" class="btn btn-danger">Stop Streaming</button>
    }
    else
    {
        <button @onclick="StartStreaming" class="btn btn-primary">Start Streaming</button>
    }
</div>

@code {
    private List<UserOnlineItem> Users = new();
    private int UsersOnlineCount => Users.Count;
    private bool IsStreaming = false;
    private CancellationTokenSource? _cts;

    protected override async Task OnInitializedAsync()
    {
        // Auto-start streaming when component loads
        await StartStreaming();
    }

    private async Task StartStreaming()
    {
        IsStreaming = true;
        _cts = new CancellationTokenSource();
        Users.Clear();

        try
        {
            await foreach (var user in WorkflowClient.GetUsersOnlineStream())
            {
                Users.Add(user);
                await InvokeAsync(StateHasChanged); // Update UI for each item
                
                // Optional: Limit updates to avoid excessive re-renders
                if (Users.Count % 10 == 0)
                {
                    await Task.Delay(100); // Small delay to batch updates
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Streaming error: {ex.Message}");
        }
        finally
        {
            IsStreaming = false;
        }
    }

    private void StopStreaming()
    {
        _cts?.Cancel();
        IsStreaming = false;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

### 3. Real-Time Dashboard with Streaming

```razor
@* DashboardComponent.razor - Multi-stream dashboard *@
@implements IAsyncDisposable
@using AF.ECT.Shared.Services

<div class="dashboard">
    <div class="row">
        <div class="col-md-6">
            <h4>Users Online: @UsersCount</h4>
            <StreamingDataComponent />
        </div>
        <div class="col-md-6">
            <h4>Active Cases: @CasesCount</h4>
            <CaseStreamComponent />
        </div>
    </div>
</div>

@code {
    private int UsersCount;
    private int CasesCount;

    private async Task UpdateCounts()
    {
        // Get counts via unary call (faster than streaming for status)
        var response = await _workflowClient.GetUsersOnlineAsync();
        UsersCount = response.Items.Count;
    }
}
```

### 4. Handling Backpressure in Blazor

```csharp
// SlowBlazorConsumer.razor - Handle slow client processing

@code {
    private async Task ProcessStreamWithBackpressure()
    {
        var processedCount = 0;
        const int batchSize = 50; // Process in batches
        var batch = new List<UserOnlineItem>();

        try
        {
            await foreach (var user in WorkflowClient.GetUsersOnlineStream())
            {
                batch.Add(user);
                
                if (batch.Count >= batchSize)
                {
                    // Process batch (this applies backpressure to stream)
                    await ProcessBatchAsync(batch);
                    processedCount += batch.Count;
                    
                    batch.Clear();
                    
                    // Allow UI to update
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(100); // Backpressure
                }
            }
            
            // Process remaining
            if (batch.Count > 0)
            {
                await ProcessBatchAsync(batch);
                processedCount += batch.Count;
            }
        }
        catch (OperationCanceledException)
        {
            // User cancelled streaming
        }
    }

    private async Task ProcessBatchAsync(List<UserOnlineItem> batch)
    {
        // Your processing logic here
        await Task.CompletedTask;
    }
}
```

---

## Rate Limiting Configuration

### 1. Configure Rate Limiter in DI

```csharp
// Program.cs - Register rate limiter and health checks

var streamingRateLimitOptions = new StreamingRateLimitingOptions
{
    MaxConcurrentStreams = 100,
    MaxItemsPerSecond = 10000,
    AcquireTimeoutMs = 5000,
    Enabled = true
};

services.AddSingleton(streamingRateLimitOptions);
services.AddSingleton<StreamingRateLimiter>();

// Register streaming health check
services.AddHealthChecks()
    .AddStreamingHealthCheck(
        name: "streaming",
        tags: new[] { "grpc", "streaming", "critical" });
```

### 2. Apply Rate Limiting to Streaming Methods

```csharp
// In WorkflowClient.cs - Wrap streaming calls with rate limiting

private readonly StreamingRateLimiter _rateLimiter;

public async IAsyncEnumerable<UserOnlineItem> GetUsersOnlineStream()
{
    // Acquire concurrency permit
    using var concurrencyLease = await _rateLimiter.AcquireConcurrencyPermitAsync();
    
    if (!concurrencyLease.IsSuccessful)
    {
        throw new InvalidOperationException("Max concurrent streams exceeded");
    }

    using var call = _client.GetUsersOnlineStream(new EmptyRequest());
    var itemCount = 0;

    while (await call.ResponseStream.MoveNext(CancellationToken.None))
    {
        // Acquire throughput permit per item
        using var throughputLease = await _rateLimiter.AcquireThroughputPermitAsync(1);
        
        if (throughputLease.IsSuccessful)
        {
            yield return call.ResponseStream.Current;
            itemCount++;
        }
    }
}
```

---

## Health Check Monitoring

### 1. Add Health Check Endpoint

```csharp
// Program.cs - Configure health check endpoint

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Detailed streaming health check
app.MapHealthChecks("/health/streaming", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("streaming"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            details = report.Entries.ToDictionary(
                x => x.Key,
                x => new
                {
                    status = x.Value.Status.ToString(),
                    duration = x.Value.Duration.TotalMilliseconds,
                    data = x.Value.Data
                })
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});
```

### 2. Monitor Health Check Results

```csharp
// Client-side health monitoring
public class StreamingHealthMonitor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<StreamingHealthMonitor> _logger;

    public async Task<HealthStatus> CheckStreamingHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health/streaming");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsAsync<HealthCheckResponse>();
                return content.Status switch
                {
                    "Healthy" => HealthStatus.Healthy,
                    "Degraded" => HealthStatus.Degraded,
                    _ => HealthStatus.Unhealthy
                };
            }

            return HealthStatus.Unhealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return HealthStatus.Unhealthy;
        }
    }
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}
```

---

## Troubleshooting Common Streaming Issues

### Issue: "Connection Timeout"
**Cause:** Streaming takes longer than default timeout (30s)
**Solution:** Increase timeout in WorkflowClientOptions
```csharp
var options = new WorkflowClientOptions { RequestTimeoutSeconds = 120 };
```

### Issue: "Memory Unbounded Growth"
**Cause:** Buffering without backpressure
**Solution:** Process items immediately or use bounded channels
```csharp
// Process immediately
await foreach (var item in stream) ProcessItem(item);
```

### Issue: "Rate Limit Exceeded"
**Cause:** Too many concurrent streams or high throughput
**Solution:** Increase limits or implement client-side backoff
```csharp
var options = new StreamingRateLimitingOptions
{
    MaxConcurrentStreams = 200,
    MaxItemsPerSecond = 20000
};
```

### Issue: "Blazor Component Hanging"
**Cause:** Unbounded streaming without cancellation
**Solution:** Implement component disposal with cancellation
```csharp
async ValueTask IAsyncDisposable.DisposeAsync()
{
    _cts?.Cancel();
    _cts?.Dispose();
}
```

---

## Performance Tuning Checklist

- [ ] Database indexes optimized for streaming queries
- [ ] Connection pooling configured (Min: 5, Max: 100)
- [ ] Timeout values appropriate for expected stream duration
- [ ] Rate limiting enabled with reasonable limits
- [ ] Health checks deployed and monitored
- [ ] Blazor components properly dispose streaming tasks
- [ ] Backpressure applied (no unbounded buffering)
- [ ] Error handling for transient failures
- [ ] Distributed tracing enabled for debugging
- [ ] Performance benchmarks baseline established
