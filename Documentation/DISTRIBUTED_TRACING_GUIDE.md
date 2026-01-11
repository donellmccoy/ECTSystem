# Distributed Tracing and Observability Guide

## Overview

This guide demonstrates how to implement comprehensive distributed tracing using OpenTelemetry and correlation IDs across the ECTSystem, enabling end-to-end request tracking from Blazor client through gRPC to the database.

---

## Correlation ID Implementation

### 1. Generate and Propagate Correlation IDs

```csharp
// AF.ECT.Shared/Utilities/CorrelationIdGenerator.cs

/// <summary>
/// Generates and manages correlation IDs for end-to-end request tracing.
/// </summary>
public static class CorrelationIdGenerator
{
    /// <summary>
    /// Generates a new correlation ID or extracts from existing context.
    /// </summary>
    public static string GetOrGenerateCorrelationId(
        string? existingId = null)
    {
        if (!string.IsNullOrEmpty(existingId))
            return existingId;

        return $"corr-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Extracts correlation ID from gRPC metadata headers.
    /// </summary>
    public static string? ExtractFromGrpcMetadata(
        Metadata metadata,
        string headerName = "x-correlation-id")
    {
        var entry = metadata.FirstOrDefault(m =>
            m.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase));

        return entry?.Value;
    }

    /// <summary>
    /// Extracts correlation ID from HTTP headers.
    /// </summary>
    public static string? ExtractFromHttpHeaders(
        IHeaderDictionary headers,
        string headerName = "x-correlation-id")
    {
        return headers.TryGetValue(headerName, out var value)
            ? value.ToString()
            : null;
    }
}
```

### 2. Correlation ID Middleware (Server)

```csharp
// AF.ECT.Server/Middleware/CorrelationIdMiddleware.cs

/// <summary>
/// Middleware that adds correlation ID to HTTP context and logs.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "x-correlation-id";

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = CorrelationIdGenerator
            .ExtractFromHttpHeaders(context.Request.Headers, CorrelationIdHeader)
            ?? CorrelationIdGenerator.GetOrGenerateCorrelationId();

        // Add to request context
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Add to structured logging
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "RequestPath", context.Request.Path },
            { "RequestMethod", context.Request.Method }
        }))
        {
            await _next(context);
        }
    }
}

// Program.cs - Register middleware
app.UseMiddleware<CorrelationIdMiddleware>();
```

### 3. Correlation ID in gRPC Services

```csharp
// AF.ECT.Server/Services/WorkflowServiceImpl.cs

using Grpc.Core;

/// <summary>
/// Workflow service implementation with correlation ID tracking.
/// </summary>
public class WorkflowServiceImpl : WorkflowService.WorkflowServiceBase
{
    private readonly ILogger<WorkflowServiceImpl> _logger;
    private readonly ECTContext _context;

    public override async Task<GetWorkflowResponse> GetWorkflow(
        GetWorkflowRequest request,
        ServerCallContext context)
    {
        // Extract or generate correlation ID
        var correlationId = CorrelationIdGenerator
            .ExtractFromGrpcMetadata(context.RequestHeaders)
            ?? CorrelationIdGenerator.GetOrGenerateCorrelationId();

        // Add to response headers
        context.ResponseTrailers.Add("x-correlation-id", correlationId);

        // Use for structured logging
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "MethodName", nameof(GetWorkflow) },
            { "WorkflowId", request.Id }
        }))
        {
            try
            {
                _logger.LogInformation("Retrieving workflow {WorkflowId}", request.Id);

                var workflow = await _context.Workflows
                    .AsNoTracking()
                    .FirstOrDefaultAsync(w => w.Id == request.Id);

                if (workflow == null)
                {
                    _logger.LogWarning("Workflow not found");
                    throw new RpcException(
                        new Status(StatusCode.NotFound, $"Workflow {request.Id} not found"));
                }

                _logger.LogInformation("Successfully retrieved workflow");
                return MapToResponse(workflow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving workflow");
                throw;
            }
        }
    }

    public override async Task GetUsersOnlineStream(
        EmptyRequest request,
        IServerStreamWriter<UserOnlineItem> responseStream,
        ServerCallContext context)
    {
        var correlationId = CorrelationIdGenerator
            .ExtractFromGrpcMetadata(context.RequestHeaders)
            ?? CorrelationIdGenerator.GetOrGenerateCorrelationId();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "MethodName", nameof(GetUsersOnlineStream) },
            { "StreamType", "GetUsersOnline" }
        }))
        {
            try
            {
                _logger.LogInformation("Starting streaming of online users");

                var users = _context.Users
                    .Where(u => u.IsOnline)
                    .AsAsyncEnumerable();

                int sentCount = 0;
                await foreach (var user in users)
                {
                    await responseStream.WriteAsync(new UserOnlineItem
                    {
                        UserId = user.Id,
                        UserName = user.Name,
                        LastActive = Timestamp.FromDateTime(user.LastActive)
                    });

                    sentCount++;
                    if (sentCount % 100 == 0)
                    {
                        _logger.LogInformation(
                            "Streamed {Count} users", sentCount);
                    }
                }

                _logger.LogInformation(
                    "Completed streaming, sent {Count} users", sentCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during streaming");
                throw;
            }
        }
    }
}
```

### 4. Correlation ID in gRPC Client

```csharp
// AF.ECT.Shared/Services/WorkflowClient.cs

/// <summary>
/// gRPC client with correlation ID propagation.
/// </summary>
public class WorkflowClient
{
    private readonly WorkflowService.WorkflowServiceClient _client;
    private readonly ILogger<WorkflowClient> _logger;
    private string? _currentCorrelationId;

    public WorkflowClient(
        WorkflowService.WorkflowServiceClient client,
        ILogger<WorkflowClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <summary>
    /// Sets the current correlation ID for this client instance.
    /// </summary>
    public void SetCorrelationId(string? correlationId)
    {
        _currentCorrelationId = correlationId
            ?? CorrelationIdGenerator.GetOrGenerateCorrelationId();
    }

    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    public string GetCorrelationId() => _currentCorrelationId
        ?? CorrelationIdGenerator.GetOrGenerateCorrelationId();

    private Metadata BuildMetadata()
    {
        var metadata = new Metadata
        {
            { "x-correlation-id", GetCorrelationId() }
        };
        return metadata;
    }

    public async Task<GetWorkflowResponse> GetWorkflowAsync(string workflowId)
    {
        var correlationId = GetCorrelationId();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "MethodName", nameof(GetWorkflowAsync) }
        }))
        {
            try
            {
                _logger.LogInformation("Calling GetWorkflow for {WorkflowId}", workflowId);

                var result = await _client.GetWorkflowAsync(
                    new GetWorkflowRequest { Id = workflowId },
                    headers: BuildMetadata());

                _logger.LogInformation("Successfully retrieved workflow");
                return result;
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error in GetWorkflowAsync");
                throw;
            }
        }
    }

    public async IAsyncEnumerable<UserOnlineItem> GetUsersOnlineStream()
    {
        var correlationId = GetCorrelationId();

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", correlationId },
            { "MethodName", nameof(GetUsersOnlineStream) }
        }))
        {
            _logger.LogInformation("Starting GetUsersOnlineStream");

            using var call = _client.GetUsersOnlineStream(
                new EmptyRequest(),
                headers: BuildMetadata());

            int receivedCount = 0;
            try
            {
                while (await call.ResponseStream.MoveNext(CancellationToken.None))
                {
                    yield return call.ResponseStream.Current;
                    receivedCount++;

                    if (receivedCount % 100 == 0)
                    {
                        _logger.LogInformation(
                            "Received {Count} items from stream", receivedCount);
                    }
                }

                _logger.LogInformation(
                    "Stream completed, received {Count} total items", receivedCount);
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex, "gRPC error in GetUsersOnlineStream");
                throw;
            }
        }
    }
}
```

### 5. Correlation ID in Blazor Components

```razor
@* AF.ECT.WebClient/Pages/WorkflowDetail.razor *@
@implements IAsyncDisposable
@using AF.ECT.Shared.Services
@using AF.ECT.Shared.Utilities

@inject WorkflowClient WorkflowClient
@inject ILogger<WorkflowDetail> Logger

<div class="workflow-detail">
    <h2>Workflow: @Workflow?.Id</h2>
    <p class="text-muted">Correlation ID: @CorrelationId</p>
    
    @if (Workflow != null)
    {
        <p>Status: @Workflow.Status</p>
        <p>Created: @Workflow.CreatedDate</p>
    }
    else if (IsLoading)
    {
        <p>Loading workflow...</p>
    }
    else
    {
        <p class="text-danger">Failed to load workflow</p>
    }
</div>

@code {
    [Parameter]
    public string WorkflowId { get; set; } = string.Empty;

    private GetWorkflowResponse? Workflow;
    private string CorrelationId = string.Empty;
    private bool IsLoading = true;

    protected override async Task OnInitializedAsync()
    {
        // Generate correlation ID for this component session
        CorrelationId = CorrelationIdGenerator.GetOrGenerateCorrelationId();
        
        // Set it in the client
        WorkflowClient.SetCorrelationId(CorrelationId);

        Logger.LogInformation(
            "Loading workflow {WorkflowId} with correlation {CorrelationId}",
            WorkflowId, CorrelationId);

        try
        {
            Workflow = await WorkflowClient.GetWorkflowAsync(WorkflowId);
            IsLoading = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading workflow");
            IsLoading = false;
        }
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        Logger.LogInformation(
            "Component disposed, correlation ID {CorrelationId}",
            CorrelationId);
    }
}
```

---

## OpenTelemetry Integration

### 1. Configure OpenTelemetry in Program.cs

```csharp
// AF.ECT.Server/Program.cs

var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetry
builder.AddServiceDefaults();

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            // Filter out health checks
            options.Filter = httpContext =>
                !httpContext.Request.Path.StartsWithSegments("/health");
        })
        .AddGrpcClientInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSqlClientInstrumentation(options =>
        {
            options.SetDbStatementForText = true;
        })
        .AddSource("AF.ECT.Server")
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProcessInstrumentation()
        .AddRuntimeInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));

var app = builder.Build();

// Add correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

app.Run();
```

### 2. Create Custom Activities

```csharp
// AF.ECT.Server/Services/WorkflowService.cs

using System.Diagnostics;

/// <summary>
/// Workflow service with custom activity tracing.
/// </summary>
public class WorkflowService
{
    private static readonly ActivitySource ActivitySource =
        new ActivitySource("AF.ECT.Server.WorkflowService");

    private readonly ECTContext _context;
    private readonly ILogger<WorkflowService> _logger;

    public async Task<Workflow?> GetWorkflowByIdAsync(string id)
    {
        // Start a custom activity
        using var activity = ActivitySource.StartActivity("GetWorkflowById");
        activity?.SetTag("workflow.id", id);

        try
        {
            var startTime = Stopwatch.GetTimestamp();

            var workflow = await _context.Workflows
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id);

            var duration = Stopwatch.GetElapsedTime(startTime);
            activity?.SetTag("db.duration_ms", duration.TotalMilliseconds);

            if (workflow != null)
            {
                activity?.SetTag("workflow.found", true);
                activity?.SetTag("workflow.status", workflow.Status);
            }
            else
            {
                activity?.SetTag("workflow.found", false);
                activity?.SetStatus(ActivityStatusCode.Error, "Workflow not found");
            }

            return workflow;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }

    public async IAsyncEnumerable<Workflow> GetWorkflowsStreamAsync(
        string? filterStatus = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity("GetWorkflowsStream");
        activity?.SetTag("filter.status", filterStatus ?? "all");

        var query = _context.Workflows.AsAsyncEnumerable();

        if (!string.IsNullOrEmpty(filterStatus))
        {
            query = query.Where(w => w.Status == filterStatus);
        }

        int yieldCount = 0;
        var startTime = Stopwatch.GetTimestamp();

        try
        {
            await foreach (var workflow in query.WithCancellation(cancellationToken))
            {
                yield return workflow;
                yieldCount++;

                if (yieldCount % 100 == 0)
                {
                    activity?.SetTag("items_yielded", yieldCount);
                }
            }

            var duration = Stopwatch.GetElapsedTime(startTime);
            activity?.SetTag("total_items", yieldCount);
            activity?.SetTag("stream_duration_ms", duration.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            activity?.SetStatus(
                ActivityStatusCode.Error,
                "Stream cancelled");
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

### 3. Trace Database Operations

```csharp
// AF.ECT.Data/DatabaseTraceInterceptor.cs

using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Diagnostics;

/// <summary>
/// EF Core interceptor for tracing database operations.
/// </summary>
public class DatabaseTraceInterceptor : DbCommandInterceptor
{
    private static readonly ActivitySource ActivitySource =
        new ActivitySource("AF.ECT.Data");

    private readonly ILogger<DatabaseTraceInterceptor> _logger;

    public DatabaseTraceInterceptor(ILogger<DatabaseTraceInterceptor> logger)
    {
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(
            "ExecuteReader",
            ActivityKind.Internal);

        var commandText = command.CommandText;
        activity?.SetTag("db.statement", commandText);
        activity?.SetTag("db.system", "mssql");
        activity?.SetTag("db.operation", GetOperationType(commandText));

        return await base.ReaderExecutingAsync(
            command, eventData, result, cancellationToken);
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySource.StartActivity(
            "ExecuteNonQuery",
            ActivityKind.Internal);

        activity?.SetTag("db.statement", command.CommandText);
        activity?.SetTag("db.operation", GetOperationType(command.CommandText));

        return await base.NonQueryExecutingAsync(
            command, eventData, result, cancellationToken);
    }

    private static string GetOperationType(string commandText)
    {
        return commandText.TrimStart().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.ToUpperInvariant() ?? "UNKNOWN";
    }
}

// Program.cs - Register interceptor
services.AddScoped<DatabaseTraceInterceptor>();
services.AddDbContext<ECTContext>((provider, options) =>
{
    options.AddInterceptors(provider.GetRequiredService<DatabaseTraceInterceptor>());
});
```

### 4. Trace gRPC Client Calls

```csharp
// AF.ECT.Shared/Services/WorkflowClient.cs - Enhanced with tracing

using System.Diagnostics;

public class WorkflowClient
{
    private static readonly ActivitySource ActivitySource =
        new ActivitySource("AF.ECT.Shared.WorkflowClient");

    public async Task<GetWorkflowResponse> GetWorkflowAsync(string workflowId)
    {
        using var activity = ActivitySource.StartActivity(
            "GetWorkflow",
            ActivityKind.Client);

        activity?.SetTag("rpc.system", "grpc");
        activity?.SetTag("rpc.service", "WorkflowService");
        activity?.SetTag("rpc.method", "GetWorkflow");
        activity?.SetTag("workflow.id", workflowId);

        var startTime = Stopwatch.GetTimestamp();

        try
        {
            var result = await _client.GetWorkflowAsync(
                new GetWorkflowRequest { Id = workflowId },
                headers: BuildMetadata());

            var duration = Stopwatch.GetElapsedTime(startTime);
            activity?.SetTag("rpc.duration_ms", duration.TotalMilliseconds);
            activity?.SetTag("rpc.status", "OK");

            return result;
        }
        catch (RpcException ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("rpc.status", ex.StatusCode.ToString());
            throw;
        }
    }
}
```

---

## Observability Dashboard

### View Traces in Jaeger UI

Access traces at `http://localhost:16686` (when running Jaeger collector).

Example queries:

- By correlation ID: `tags:CorrelationId="corr-20240101120000-abc123def456"`
- By service: `service.name="AF.ECT.Server"`
- Slow requests: `duration > 1000ms`

### Example Distributed Trace

```
Request in Blazor Client
├─ CorrelationId: corr-20240101120000-abc123def456
├─ Span: GetWorkflowAsync (WorkflowClient)
│  └─ gRPC Call: GetWorkflow
│     └─ ServerCallContext: Received correlation ID
│        └─ ActivitySource: WorkflowServiceImpl.GetWorkflow
│           └─ DatabaseTraceInterceptor: SELECT from Workflows table
│              └─ SQL Server Query Execution (3.2ms)
│           └─ Response sent (5.1ms total)
│        └─ Response headers include x-correlation-id
│     └─ Client received response (6.8ms total)
│  └─ Result displayed in Blazor component
```

---

## Best Practices

1. **Always propagate correlation IDs** across service boundaries
2. **Log correlation ID** in every structured log entry
3. **Include activity duration** in traces for performance analysis
4. **Set meaningful tags** on activities for filtering/searching
5. **Record exceptions** with full context for debugging
6. **Implement sampling** in production to reduce overhead
7. **Correlate logs with traces** for full observability
8. **Monitor trace latencies** to detect performance regressions

---

## Troubleshooting

### Traces not appearing in Jaeger

- Verify OTLP collector is running on `http://localhost:4317`
- Check `AddOtlpExporter()` configuration
- Enable console exporter temporarily for debugging

### Missing correlation IDs

- Verify middleware is registered before other middleware
- Check gRPC metadata propagation in client calls
- Inspect HTTP headers in browser dev tools

### High overhead from tracing

- Enable sampling: `.AddOtlpExporter(o => o.SamplingRatio = 0.1)`
- Filter out health check endpoints
- Exclude noisy operations from tracing
