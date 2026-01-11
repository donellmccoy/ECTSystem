namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Helper for testing Polly resilience policy configurations and behaviors.
/// Provides validators for retry policies, circuit breakers, timeouts, and fallback strategies.
/// </summary>
public static class ResiliencePolicyTestHelper
{
    /// <summary>
    /// Represents the state of a circuit breaker.
    /// </summary>
    public enum CircuitBreakerState
    {
        /// <summary>
        /// Circuit is closed (normal operation, requests pass through).
        /// </summary>
        Closed,

        /// <summary>
        /// Circuit is open (failures exceeded threshold, requests fail immediately).
        /// </summary>
        Open,

        /// <summary>
        /// Circuit is half-open (testing if service recovered, limited requests pass through).
        /// </summary>
        HalfOpen
    }

    /// <summary>
    /// Validates that a retry policy has the expected configuration.
    /// </summary>
    public class RetryPolicyValidator
    {
        private readonly int _expectedMaxRetries;
        private readonly TimeSpan? _expectedInitialDelay;
        private int _actualRetries;
        private List<TimeSpan> _retryDelays = [];

        /// <summary>
        /// Initializes a new instance of the RetryPolicyValidator.
        /// </summary>
        /// <param name="expectedMaxRetries">Expected maximum number of retries.</param>
        /// <param name="expectedInitialDelay">Expected initial delay between retries (optional).</param>
        public RetryPolicyValidator(int expectedMaxRetries, TimeSpan? expectedInitialDelay = null)
        {
            _expectedMaxRetries = expectedMaxRetries;
            _expectedInitialDelay = expectedInitialDelay;
        }

        /// <summary>
        /// Records a retry attempt with its delay.
        /// </summary>
        public void RecordRetry(TimeSpan delay)
        {
            _actualRetries++;
            _retryDelays.Add(delay);
        }

        /// <summary>
        /// Validates that retry count matches expected.
        /// </summary>
        public bool ValidateRetryCount() => _actualRetries <= _expectedMaxRetries;

        /// <summary>
        /// Validates that exponential backoff timing is correct.
        /// </summary>
        /// <remarks>
        /// Exponential backoff: delay_n = initialDelay * (multiplier ^ n)
        /// </remarks>
        public bool ValidateExponentialBackoff(double multiplier = 2.0)
        {
            if (_expectedInitialDelay is null || _retryDelays.Count == 0)
                return true;

            for (int i = 0; i < _retryDelays.Count; i++)
            {
                var expectedDelay = TimeSpan.FromMilliseconds(
                    _expectedInitialDelay.Value.TotalMilliseconds * Math.Pow(multiplier, i));

                // Allow 10% tolerance for timing variations
                var tolerance = expectedDelay.TotalMilliseconds * 0.1;
                var actualMs = _retryDelays[i].TotalMilliseconds;

                if (Math.Abs(actualMs - expectedDelay.TotalMilliseconds) > tolerance)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets a summary of retry behavior for assertions.
        /// </summary>
        public string GetSummary() => 
            $"Max retries: {_actualRetries}/{_expectedMaxRetries}, Delays: {string.Join(", ", _retryDelays.Select(d => d.TotalMilliseconds + "ms"))}";
    }

    /// <summary>
    /// Validates circuit breaker state transitions and behavior.
    /// </summary>
    public class CircuitBreakerStateValidator
    {
        private CircuitBreakerState _currentState = CircuitBreakerState.Closed;
        private int _failureCount;
        private int _successCount;
        private readonly int _failureThreshold;
        private readonly int _successThresholdForReset;

        /// <summary>
        /// Initializes a new instance of the CircuitBreakerStateValidator.
        /// </summary>
        /// <param name="failureThreshold">Number of failures before opening circuit.</param>
        /// <param name="successThresholdForReset">Number of successes in half-open state to close circuit.</param>
        public CircuitBreakerStateValidator(int failureThreshold, int successThresholdForReset = 1)
        {
            _failureThreshold = failureThreshold;
            _successThresholdForReset = successThresholdForReset;
        }

        /// <summary>
        /// Records a failure and updates circuit state accordingly.
        /// </summary>
        public void RecordFailure()
        {
            _failureCount++;

            if (_currentState == CircuitBreakerState.Closed && _failureCount >= _failureThreshold)
                _currentState = CircuitBreakerState.Open;
            else if (_currentState == CircuitBreakerState.HalfOpen)
                _currentState = CircuitBreakerState.Open;
        }

        /// <summary>
        /// Records a success and updates circuit state accordingly.
        /// </summary>
        public void RecordSuccess()
        {
            _successCount++;

            if (_currentState == CircuitBreakerState.HalfOpen && _successCount >= _successThresholdForReset)
            {
                _currentState = CircuitBreakerState.Closed;
                _failureCount = 0;
                _successCount = 0;
            }
        }

        /// <summary>
        /// Transitions circuit to half-open state (for testing recovery).
        /// </summary>
        public void TransitionToHalfOpen()
        {
            if (_currentState == CircuitBreakerState.Open)
            {
                _currentState = CircuitBreakerState.HalfOpen;
                _successCount = 0;
            }
        }

        /// <summary>
        /// Gets the current circuit state.
        /// </summary>
        public CircuitBreakerState GetCurrentState() => _currentState;

        /// <summary>
        /// Validates that circuit is in the expected state.
        /// </summary>
        public bool ValidateState(CircuitBreakerState expected) => _currentState == expected;

        /// <summary>
        /// Gets a summary of circuit behavior for assertions.
        /// </summary>
        public string GetSummary() => 
            $"State: {_currentState}, Failures: {_failureCount}, Successes: {_successCount}";
    }

    /// <summary>
    /// Validates fallback behavior and timeout configurations.
    /// </summary>
    public class FallbackBehaviorValidator
    {
        private readonly object? _fallbackValue;
        private int _fallbackInvocations;
        private DateTime? _firstFallbackTime;

        /// <summary>
        /// Initializes a new instance of the FallbackBehaviorValidator.
        /// </summary>
        /// <param name="fallbackValue">The expected fallback value.</param>
        public FallbackBehaviorValidator(object? fallbackValue)
        {
            _fallbackValue = fallbackValue;
        }

        /// <summary>
        /// Records a fallback invocation.
        /// </summary>
        public void RecordFallbackInvocation()
        {
            _fallbackInvocations++;
            _firstFallbackTime ??= DateTime.UtcNow;
        }

        /// <summary>
        /// Validates that fallback was invoked the expected number of times.
        /// </summary>
        public bool ValidateFallbackInvocationCount(int expectedCount) => _fallbackInvocations == expectedCount;

        /// <summary>
        /// Validates that timeout before first fallback is within acceptable range.
        /// </summary>
        public bool ValidateTimeoutBefore(TimeSpan maxDuration) =>
            _firstFallbackTime is null || (DateTime.UtcNow - _firstFallbackTime) <= maxDuration;

        /// <summary>
        /// Gets the number of times fallback was invoked.
        /// </summary>
        public int GetFallbackInvocationCount() => _fallbackInvocations;

        /// <summary>
        /// Gets the time of first fallback invocation.
        /// </summary>
        public DateTime? GetFirstFallbackTime() => _firstFallbackTime;

        /// <summary>
        /// Gets a summary of fallback behavior for assertions.
        /// </summary>
        public string GetSummary() => 
            $"Fallback invocations: {_fallbackInvocations}, First invocation: {_firstFallbackTime:O}";
    }

    /// <summary>
    /// Creates a mock action that simulates failures for testing retry behavior.
    /// </summary>
    public static Action<int> CreateFailingAction(int failuresBeforeSuccess = 0)
    {
        var failures = 0;
        return attempt =>
        {
            if (failures < failuresBeforeSuccess)
            {
                failures++;
                throw new InvalidOperationException($"Simulated failure on attempt {attempt}");
            }
        };
    }

    /// <summary>
    /// Creates a mock function that simulates failures for testing retry behavior.
    /// </summary>
    public static Func<int, T> CreateFailingFunction<T>(T successValue, int failuresBeforeSuccess = 0)
    {
        var failures = 0;
        return attempt =>
        {
            if (failures < failuresBeforeSuccess)
            {
                failures++;
                throw new InvalidOperationException($"Simulated failure on attempt {attempt}");
            }
            return successValue;
        };
    }

    /// <summary>
    /// Creates a mock async function that simulates failures for testing async retry behavior.
    /// </summary>
    public static Func<int, Task<T>> CreateFailingAsyncFunction<T>(T successValue, int failuresBeforeSuccess = 0)
    {
        var failures = 0;
        return async attempt =>
        {
            if (failures < failuresBeforeSuccess)
            {
                failures++;
                await Task.Delay(10); // Simulate async work
                throw new InvalidOperationException($"Simulated failure on attempt {attempt}");
            }
            return successValue;
        };
    }

    /// <summary>
    /// Creates a timeout validator that checks if operation completes within expected duration.
    /// </summary>
    public static Func<Func<Task>, TimeSpan, Task<bool>> CreateTimeoutValidator()
    {
        return async (operation, timeout) =>
        {
            try
            {
                var task = operation();
                return await Task.WhenAny(task, Task.Delay(timeout)) == task;
            }
            catch
            {
                return false;
            }
        };
    }
}
