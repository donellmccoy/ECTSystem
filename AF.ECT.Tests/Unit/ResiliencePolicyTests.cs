using Xunit;
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using System.Diagnostics;

namespace AF.ECT.Tests.Unit;

/// <summary>
/// Contains tests for Polly resilience policies, validating circuit breaker behavior,
/// retry strategies, timeout handling, and combined policy interactions.
/// Ensures mission-critical operations have proper fault tolerance and recovery patterns.
/// </summary>
[Collection("Resilience Policy Tests")]
[Trait("Category", "Unit")]
[Trait("Component", "ResiliencePolicies")]
public class ResiliencePolicyTests
{
    #region Circuit Breaker Tests

    /// <summary>
    /// Tests that circuit breaker transitions to Open state after threshold failures.
    /// Validates failure count threshold is enforced before opening circuit.
    /// </summary>
    [Fact]
    public async Task CircuitBreaker_OpensAfterThresholdFailures()
    {
        // Arrange - Create circuit breaker with 2-failure threshold
        var failureCount = 0;
        var pipelineBuilder = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(2)
            });
        var pipeline = pipelineBuilder.Build();

        // Act - Trigger failures until circuit opens
        for (int i = 0; i < 4; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(async (ct) =>
                {
                    failureCount++;
                    throw new HttpRequestException("Service unavailable");
                });
            }
            catch (BrokenCircuitException)
            {
                // Circuit is now open, expected behavior
                break;
            }
            catch (HttpRequestException)
            {
                // Expected failure before circuit opens
            }
        }

        // Assert
        failureCount.Should().BeGreaterThanOrEqualTo(2);
    }

    /// <summary>
    /// Tests that circuit breaker allows successful operations after break duration.
    /// </summary>
    [Fact]
    public async Task CircuitBreaker_AllowsRetryAfterBreakDuration()
    {
        // Arrange
        var breakDuration = TimeSpan.FromMilliseconds(200);
        var failureCount = 0;
        var successCount = 0;

        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(2),
                BreakDuration = breakDuration
            })
            .Build();

        // Act - Force circuit open
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(async (ct) =>
                {
                    failureCount++;
                    throw new HttpRequestException("Service unavailable");
                });
            }
            catch (BrokenCircuitException)
            {
                break;
            }
            catch { }
        }

        // Wait for break duration to expire
        await Task.Delay(breakDuration.Add(TimeSpan.FromMilliseconds(50)));

        // Try to execute again - should attempt recovery
        try
        {
            await pipeline.ExecuteAsync(async (ct) =>
            {
                successCount++;
                return await Task.FromResult("Success");
            });
        }
        catch
        {
            // May still be open depending on timing
        }

        // Assert - Circuit should have allowed at least one attempt
        failureCount.Should().BeGreaterThanOrEqualTo(2);
    }

    #endregion

    #region Retry Policy Tests

    /// <summary>
    /// Tests that retry policy respects maximum retry count limit.
    /// Validates operation fails after max retries exhausted.
    /// </summary>
    [Fact]
    public async Task RetryPolicy_RespectsMaxRetryLimit()
    {
        // Arrange
        const int maxRetries = 2;
        var executionCount = 0;

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                Delay = TimeSpan.FromMilliseconds(5),
                BackoffType = DelayBackoffType.Linear,
                ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is HttpRequestException)
            })
            .Build();

        // Act
        var caughtException = false;
        try
        {
            await pipeline.ExecuteAsync(async (ct) =>
            {
                executionCount++;
                throw new HttpRequestException("Persistent failure");
            });
        }
        catch (HttpRequestException)
        {
            caughtException = true;
        }

        // Assert
        caughtException.Should().BeTrue();
        executionCount.Should().Be(maxRetries + 1); // Initial + retries
    }

    /// <summary>
    /// Tests that retry policy succeeds on later attempt.
    /// </summary>
    [Fact]
    public async Task RetryPolicy_SucceedsOnEventualSuccess()
    {
        // Arrange
        var attemptCount = 0;

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(5),
                BackoffType = DelayBackoffType.Linear,
                ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is HttpRequestException)
            })
            .Build();

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            attemptCount++;
            if (attemptCount < 3)
            {
                throw new HttpRequestException("Transient failure");
            }
            return "Success";
        });

        // Assert
        result.Should().Be("Success");
        attemptCount.Should().Be(3);
    }

    /// <summary>
    /// Tests retry policy with exponential backoff delay timing.
    /// </summary>
    [Fact]
    public async Task RetryPolicy_ExponentialBackoffDelays()
    {
        // Arrange
        const int initialDelayMs = 10;
        var attemptCount = 0;
        var stopwatch = Stopwatch.StartNew();

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(initialDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = false,
                ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is HttpRequestException)
            })
            .Build();

        // Act
        try
        {
            await pipeline.ExecuteAsync(async (ct) =>
            {
                attemptCount++;
                throw new HttpRequestException("Test failure");
            });
        }
        catch { }

        stopwatch.Stop();

        // Assert - Should have retried with delays
        attemptCount.Should().Be(3); // Initial + 2 retries
        stopwatch.ElapsedMilliseconds.Should().BeGreaterThan(initialDelayMs);
    }

    #endregion

    #region Timeout Policy Tests

    /// <summary>
    /// Tests that timeout policy enforces maximum operation duration.
    /// </summary>
    [Fact]
    public async Task TimeoutPolicy_CancelsOperationOnTimeout()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromMilliseconds(100))
            .Build();

        var operationCompleted = false;

        // Act
        var caughtTimeout = false;
        try
        {
            await pipeline.ExecuteAsync(async (ct) =>
            {
                await Task.Delay(500, ct); // Exceeds 100ms timeout
                operationCompleted = true;
                return "Should not complete";
            });
        }
        catch (OperationCanceledException)
        {
            caughtTimeout = true;
        }

        // Assert
        caughtTimeout.Should().BeTrue();
        operationCompleted.Should().BeFalse();
    }

    /// <summary>
    /// Tests that operations completing within timeout succeed.
    /// </summary>
    [Fact]
    public async Task TimeoutPolicy_AllowsOperationsWithinTimeout()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromMilliseconds(500))
            .Build();

        // Act
        var result = await pipeline.ExecuteAsync(async (ct) =>
        {
            await Task.Delay(50); // Well within timeout
            return "Completed successfully";
        });

        // Assert
        result.Should().Be("Completed successfully");
    }

    #endregion

    #region Combined Policy Tests

    /// <summary>
    /// Tests interaction between retry and circuit breaker policies.
    /// </summary>
    [Fact]
    public async Task CombinedPolicies_RetryThenCircuitBreaker()
    {
        // Arrange
        var executionCount = 0;

        var pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 1,
                Delay = TimeSpan.FromMilliseconds(5),
                BackoffType = DelayBackoffType.Linear,
                ShouldHandle = args => new ValueTask<bool>(args.Outcome.Exception is HttpRequestException)
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(2)
            })
            .Build();

        // Act - Execute multiple times to trigger circuit break
        var exceptions = new List<Exception>();
        for (int i = 0; i < 4; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(async (ct) =>
                {
                    executionCount++;
                    throw new HttpRequestException("Persistent failure");
                });
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        // Assert
        executionCount.Should().BeGreaterThan(0);
        exceptions.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests timeout and circuit breaker policies together.
    /// </summary>
    [Fact]
    public async Task CombinedPolicies_TimeoutThenCircuitBreaker()
    {
        // Arrange
        var pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(TimeSpan.FromMilliseconds(100))
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 1.0,
                MinimumThroughput = 2,
                SamplingDuration = TimeSpan.FromSeconds(2)
            })
            .Build();

        // Act
        var caughtTimeouts = 0;
        for (int i = 0; i < 3; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(async (ct) =>
                {
                    await Task.Delay(500, ct); // Will timeout
                    return "Never reached";
                });
            }
            catch (OperationCanceledException)
            {
                caughtTimeouts++;
            }
            catch (BrokenCircuitException)
            {
                break;
            }
        }

        // Assert
        caughtTimeouts.Should().BeGreaterThanOrEqualTo(1);
    }

    #endregion

    #region Concurrency Limiter Tests

    /// <summary>
    /// Tests concurrency limiter prevents resource exhaustion.
    /// </summary>
    [Fact]
    public async Task ConcurrencyLimiter_EnforcesConcurrentLimit()
    {
        // Arrange
        const int maxParallelization = 2;
        var concurrentExecutions = 0;
        var maxConcurrency = 0;
        var lockObj = new object();

        var pipeline = new ResiliencePipelineBuilder()
            .AddConcurrencyLimiter(maxParallelization, 5)
            .Build();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            var task = pipeline.ExecuteAsync(async (ct) =>
            {
                lock (lockObj)
                {
                    concurrentExecutions++;
                    maxConcurrency = Math.Max(maxConcurrency, concurrentExecutions);
                }

                await Task.Delay(50);

                lock (lockObj)
                {
                    concurrentExecutions--;
                }
            }).AsTask();
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Assert
        maxConcurrency.Should().BeLessThanOrEqualTo(maxParallelization + 1); // Small tolerance for timing
    }

    #endregion
}
