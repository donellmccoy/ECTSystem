# Polly Resilience Patterns Guide

This guide provides comprehensive examples of implementing resilience patterns using Polly in ECTSystem.

## Table of Contents

1. [Circuit Breaker Pattern](#circuit-breaker-pattern)
2. [Retry Pattern](#retry-pattern)
3. [Timeout Pattern](#timeout-pattern)
4. [Bulkhead (Isolation) Pattern](#bulkhead-isolation-pattern)
5. [Rate Limiting Pattern](#rate-limiting-pattern)
6. [Combining Patterns (Resilience Pipelines)](#combining-patterns-resilience-pipelines)
7. [Monitoring and Observability](#monitoring-and-observability)

---

## Circuit Breaker Pattern

### What is a Circuit Breaker?

A circuit breaker prevents cascading failures by stopping requests to a failing service. It has three states:

- **Closed**: Normal operation, requests pass through
- **Open**: Service is failing, requests fail immediately
- **Half-Open**: Testing if service recovered, allows limited requests

### Implementation Example

```csharp
// AF.ECT.Shared/Services/WorkflowClient.cs

using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

public class WorkflowClient
{
    private readonly IAsyncPolicy<GrpcResponse> _circuitBreakerPolicy;
    private readonly WorkflowService.WorkflowServiceClient _client;
    private readonly ILogger<WorkflowClient> _logger;

    public WorkflowClient(
        WorkflowService.WorkflowServiceClient client,
        ILogger<WorkflowClient> logger)
    {
        _client = client;
        _logger = logger;

        // Configure circuit breaker
        _circuitBreakerPolicy = Policy
            .Handle<RpcException>()
            .OrResult<GrpcResponse>(r => !r.Success)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    _logger.LogWarning(
                        $"Circuit breaker opened for {duration.TotalSeconds}s due to {outcome.Outcome}");
                },
                onReset: () =>
                {
                    _logger.LogInformation("Circuit breaker reset");
                },
                onHalfOpen: () =>
                {
                    _logger.LogInformation("Circuit breaker half-open, testing service");
                });
    }

    public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId)
    {
        try
        {
            var result = await _circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                // Your gRPC call here
                return await _client.GetWorkflowAsync(new GetWorkflowRequest { Id = workflowId });
            });

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError($"Service unavailable - circuit is open: {ex.Message}");
            throw;
        }
    }
}
```

### Advanced Circuit Breaker Configuration

```csharp
// Using AdvancedCircuitBreakerAsync for percentage-based failure detection

var advancedPolicy = Policy
    .Handle<RpcException>()
    .OrResult<GrpcResponse>(r => !r.Success)
    .AdvancedCircuitBreakerAsync(
        failureThreshold: 0.5, // Trip at 50% failure rate
        samplingDuration: TimeSpan.FromSeconds(10),
        minimumThroughput: 10, // Need at least 10 requests to evaluate
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            _logger.LogWarning(
                $"Circuit breaker opened for {duration.TotalSeconds}s. " +
                $"Failure rate: {outcome.Outcome}");
        });
```

---

## Retry Pattern

### Simple Retry with Exponential Backoff

```csharp
// Simple retry with exponential backoff
var retryPolicy = Policy
    .Handle<RpcException>(ex => ex.StatusCode == StatusCode.Unavailable)
    .Or<TimeoutException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            _logger.LogWarning(
                $"Retry {retryCount} after {timespan.TotalSeconds}s due to {outcome.Exception?.Message}");
        });
```

### Retry with Jitter (Randomization)

```csharp
// Prevent thundering herd with jitter
var random = new Random();
var retryPolicy = Policy
    .Handle<RpcException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: attempt =>
        {
            var exponentialBackoff = TimeSpan.FromSeconds(Math.Pow(2, attempt));
            var jitter = TimeSpan.FromMilliseconds(random.Next(0, 100));
            return exponentialBackoff.Add(jitter);
        });
```

### Retry with Max Duration

```csharp
// Retry but give up after maximum total time
var retryPolicy = Policy
    .Handle<RpcException>()
    .WaitAndRetryAsync(
        retryCount: int.MaxValue,
        sleepDurationProvider: (attempt, context) =>
        {
            var maxDuration = (TimeSpan)context["MaxDuration"];
            var elapsedTime = (TimeSpan)context.GetOrDefault("ElapsedTime", TimeSpan.Zero);
            
            if (elapsedTime > maxDuration)
                return TimeSpan.Zero; // Stop retrying
                
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        });
```

---

## Timeout Pattern

### Simple Timeout

```csharp
// Timeout after 10 seconds
var timeoutPolicy = Policy
    .TimeoutAsync<WorkflowResponse>(
        TimeSpan.FromSeconds(10),
        TimeoutStrategy.Optimistic);

public async Task<WorkflowResponse> GetWorkflowWithTimeoutAsync(string workflowId)
{
    try
    {
        return await timeoutPolicy.ExecuteAsync(async () =>
        {
            return await _client.GetWorkflowAsync(
                new GetWorkflowRequest { Id = workflowId });
        });
    }
    catch (TimeoutRejectedException ex)
    {
        _logger.LogError($"Request timed out: {ex.Message}");
        throw;
    }
}
```

### Timeout with Cancellation

```csharp
// Timeout with proper cancellation token handling
var timeoutPolicy = Policy
    .TimeoutAsync<WorkflowResponse>(
        TimeSpan.FromSeconds(30),
        TimeoutStrategy.Optimistic); // Cancels the operation

public async Task<WorkflowResponse> GetWorkflowAsync(
    string workflowId,
    CancellationToken cancellationToken = default)
{
    var policyContext = new Context
    {
        { "CancellationToken", cancellationToken }
    };

    return await timeoutPolicy.ExecuteAsync(
        (ctx, ct) => _client.GetWorkflowAsync(
            new GetWorkflowRequest { Id = workflowId },
            cancellationToken: ct),
        policyContext,
        cancellationToken);
}
```

---

## Bulkhead (Isolation) Pattern

### Limiting Concurrent Requests

```csharp
// Limit to 10 concurrent requests to prevent resource exhaustion
var bulkheadPolicy = Policy.BulkheadAsync(
    maxParallelization: 10,
    maxQueuingActions: 20, // Queue up to 20 additional requests
    onBulkheadRejectedAsync: context =>
    {
        _logger.LogWarning("Bulkhead limit reached, request queued or rejected");
        return Task.CompletedTask;
    });

public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId)
{
    try
    {
        return await bulkheadPolicy.ExecuteAsync(async () =>
        {
            return await _client.GetWorkflowAsync(
                new GetWorkflowRequest { Id = workflowId });
        });
    }
    catch (BulkheadRejectedException ex)
    {
        _logger.LogError($"Bulkhead limit exceeded: {ex.Message}");
        throw;
    }
}
```

### Named Bulkheads for Different Services

```csharp
// Register bulkheads for different services
services.AddSingleton(new BulkheadRegistry(new Dictionary<string, IAsyncPolicy>
{
    {
        "WorkflowService",
        Policy.BulkheadAsync(
            maxParallelization: 10,
            maxQueuingActions: 20)
    },
    {
        "NotificationService",
        Policy.BulkheadAsync(
            maxParallelization: 5,
            maxQueuingActions: 10)
    }
}));

// Usage
var bulkhead = _registry.GetPolicy("WorkflowService");
await bulkhead.ExecuteAsync(() => CallWorkflowService());
```

---

## Rate Limiting Pattern

### Simple Rate Limiting (Token Bucket)

```csharp
// Allow 100 requests per 10 seconds
var rateLimitPolicy = Policy.RateLimitAsync(
    numberOfExecutions: 100,
    perTimeSpan: TimeSpan.FromSeconds(10),
    numberOfParallelOperations: 1); // Sequential rate limit

public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId)
{
    return await rateLimitPolicy.ExecuteAsync(async () =>
    {
        return await _client.GetWorkflowAsync(
            new GetWorkflowRequest { Id = workflowId });
    });
}
```

### Adaptive Rate Limiting Based on Server Load

```csharp
// Adjust rate limit based on server response
public class AdaptiveRateLimiter
{
    private readonly int _maxRequests;
    private int _currentLimit;
    private readonly IAsyncPolicy _rateLimitPolicy;

    public AdaptiveRateLimiter(int initialLimit = 100)
    {
        _maxRequests = initialLimit;
        _currentLimit = initialLimit;
        UpdateRateLimitPolicy();
    }

    private void UpdateRateLimitPolicy()
    {
        // Dynamically update the policy
        _rateLimitPolicy = Policy.RateLimitAsync(
            numberOfExecutions: _currentLimit,
            perTimeSpan: TimeSpan.FromSeconds(10));
    }

    public void AdjustLimitBasedOnLoad(double cpuUsage)
    {
        if (cpuUsage > 0.8) // High load
            _currentLimit = (int)(_maxRequests * 0.5);
        else if (cpuUsage > 0.5) // Medium load
            _currentLimit = (int)(_maxRequests * 0.75);
        else // Low load
            _currentLimit = _maxRequests;

        UpdateRateLimitPolicy();
    }
}
```

---

## Combining Patterns (Resilience Pipelines)

### Using ResiliencePipeline (Polly v8+)

```csharp
// AF.ECT.Shared/Services/WorkflowClient.cs

using Polly;

public class WorkflowClient
{
    private readonly ResiliencePipeline _resilencePipeline;
    private readonly WorkflowService.WorkflowServiceClient _client;
    private readonly ILogger<WorkflowClient> _logger;

    public WorkflowClient(
        WorkflowService.WorkflowServiceClient client,
        ILogger<WorkflowClient> logger)
    {
        _client = client;
        _logger = logger;

        // Build a comprehensive resilience pipeline
        var pipelineBuilder = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(30))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<RpcException>(ex =>
                        ex.StatusCode == StatusCode.Unavailable ||
                        ex.StatusCode == StatusCode.DeadlineExceeded)
                    .Or<TimeoutException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30),
                OnOpenCircuitEvent = (outcome) =>
                {
                    _logger.LogWarning("Circuit breaker opened");
                    return default;
                }
            })
            .AddBulkhead(new BulkheadStrategyOptions
            {
                MaxParallelization = 10,
                MaxQueuingActions = 20
            });

        _resilencePipeline = pipelineBuilder.Build();
    }

    public async Task<WorkflowResponse> GetWorkflowAsync(string workflowId)
    {
        try
        {
            return await _resilencePipeline.ExecuteAsync(async () =>
            {
                return await _client.GetWorkflowAsync(
                    new GetWorkflowRequest { Id = workflowId });
            });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Operation cancelled: {ex.Message}");
            throw;
        }
    }

    public async IAsyncEnumerable<UserOnlineItem> GetUsersOnlineStream()
    {
        // For streaming, use timeout only (not retry/circuit-breaker)
        var timeoutPolicy = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromSeconds(120)) // Longer timeout for streaming
            .Build();

        using var call = _client.GetUsersOnlineStream(new EmptyRequest());
        
        while (await call.ResponseStream.MoveNext(CancellationToken.None))
        {
            yield return call.ResponseStream.Current;
        }
    }
}
```

### Builder Pattern with Extension Methods

```csharp
// AF.ECT.ServiceDefaults/Extensions.cs

using Polly;

public static class ResiliencePipelineExtensions
{
    /// <summary>
    /// Adds default resilience policies for gRPC calls.
    /// </summary>
    public static ResiliencePipelineBuilder AddGrpcDefaults(
        this ResiliencePipelineBuilder builder,
        ILogger logger)
    {
        return builder
            .AddTimeout(TimeSpan.FromSeconds(30))
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                Delay = TimeSpan.FromSeconds(1),
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<RpcException>(ex =>
                        ex.StatusCode == StatusCode.Unavailable ||
                        ex.StatusCode == StatusCode.DeadlineExceeded)
                    .Or<TimeoutException>()
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(10),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddBulkhead(new BulkheadStrategyOptions
            {
                MaxParallelization = 20,
                MaxQueuingActions = 50
            });
    }

    /// <summary>
    /// Adds default resilience policies for streaming calls.
    /// </summary>
    public static ResiliencePipelineBuilder AddStreamingDefaults(
        this ResiliencePipelineBuilder builder,
        ILogger logger)
    {
        return builder
            .AddTimeout(TimeSpan.FromSeconds(300)) // Long timeout for streaming
            .AddBulkhead(new BulkheadStrategyOptions
            {
                MaxParallelization = 5, // Fewer concurrent streams
                MaxQueuingActions = 10
            });
    }
}

// Usage in WorkflowClient
var builder = new ResiliencePipelineBuilder();
_resilencePipeline = builder
    .AddGrpcDefaults(_logger)
    .Build();
```

---

## Monitoring and Observability

### Telemetry with OpenTelemetry

```csharp
// Program.cs - Enable telemetry for Polly

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracingBuilder =>
    {
        tracingBuilder
            .AddSource("Polly")
            .AddAspNetCoreInstrumentation()
            .AddGrpcClientInstrumentation()
            .AddSqlClientInstrumentation();
    });

// Register Polly activities
var meterProvider = new MeterProviderBuilder()
    .AddMeter("Polly")
    .Build();
```

### Logging Resilience Events

```csharp
// Custom logging middleware for resilience events

public class ResilienceEventLogger
{
    private readonly ILogger<ResilienceEventLogger> _logger;

    public ResilienceEventLogger(ILogger<ResilienceEventLogger> logger)
    {
        _logger = logger;
    }

    public void OnRetry(string methodName, int attemptNumber, Exception ex)
    {
        _logger.LogWarning(
            "Retry #{RetryNumber} for {MethodName} due to {Exception}",
            attemptNumber, methodName, ex?.Message);
    }

    public void OnCircuitBreakerOpened(string methodName)
    {
        _logger.LogError(
            "Circuit breaker opened for {MethodName}",
            methodName);
    }

    public void OnBulkheadRejected(string methodName)
    {
        _logger.LogWarning(
            "Bulkhead limit reached for {MethodName}",
            methodName);
    }
}
```

### Health Check Integration

```csharp
// Monitor circuit breaker health

public class CircuitBreakerHealthCheck : IHealthCheck
{
    private readonly IAsyncPolicy _policy;

    public CircuitBreakerHealthCheck(IAsyncPolicy policy)
    {
        _policy = policy;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var circuitState = _policy is CircuitBreakerPolicy cbp
            ? cbp.CircuitState
            : CircuitState.Closed;

        return Task.FromResult(
            circuitState == CircuitState.Closed
                ? HealthCheckResult.Healthy()
                : circuitState == CircuitState.Open
                    ? HealthCheckResult.Unhealthy($"Circuit is open")
                    : HealthCheckResult.Degraded("Circuit is half-open"));
    }
}
```

---

## Best Practices

1. **Use appropriate patterns for different scenarios:**
   - Unary calls: Retry + Circuit Breaker + Timeout + Bulkhead
   - Streaming calls: Timeout + Bulkhead (not retry)

2. **Configure timeouts carefully:**
   - Unary: 10-30 seconds
   - Streaming: 5+ minutes depending on expected data volume

3. **Monitor circuit breaker state:**
   - Log when opening/closing
   - Create metrics for alerting

4. **Test resilience policies:**
   - Simulate failures (network issues, timeouts)
   - Verify fallback behavior

5. **Document policy configuration:**
   - Why specific values were chosen
   - When to adjust thresholds

6. **Use context for cross-cutting concerns:**
   - Correlation IDs
   - Request metadata
   - Cancellation tokens
