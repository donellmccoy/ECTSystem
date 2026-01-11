namespace AF.ECT.Tests.Common;

/// <summary>
/// Provides parameterized test data generation to reduce test duplication.
/// Supports testing multiple scenarios with a single test method using Theory attributes.
/// </summary>
public static class TestDataGenerator
{
    /// <summary>
    /// Generates test data for empty/null response scenarios across multiple data service calls.
    /// </summary>
    /// <returns>Enumerable of (method name, expected count) tuples</returns>
    public static IEnumerable<object[]> EmptyResponseScenarios()
    {
        yield return new object[] { "NullResponse", 0 };
        yield return new object[] { "EmptyList", 0 };
        yield return new object[] { "EmptyCollection", 0 };
    }

    /// <summary>
    /// Generates boundary value test data for integer parameters.
    /// </summary>
    /// <param name="min">Minimum valid value</param>
    /// <param name="max">Maximum valid value</param>
    /// <returns>Test data including boundary values and typical values</returns>
    public static IEnumerable<object[]> BoundaryValueTestData(int min, int max)
    {
        yield return new object[] { min, true };  // Minimum valid
        yield return new object[] { max, true };  // Maximum valid
        yield return new object[] { min - 1, false };  // Below minimum
        yield return new object[] { max + 1, false };  // Above maximum
        yield return new object[] { (min + max) / 2, true };  // Middle value
    }

    /// <summary>
    /// Generates test data for null/invalid string scenarios.
    /// </summary>
    /// <returns>Test data for string validation</returns>
    public static IEnumerable<object?[]> InvalidStringScenarios()
    {
        yield return new object?[] { null, false };  // Null
        yield return new object?[] { "", false };    // Empty
        yield return new object?[] { " ", false };   // Whitespace only
        yield return new object?[] { "Valid", true }; // Valid string
    }

    /// <summary>
    /// Generates test data for exception propagation scenarios.
    /// </summary>
    /// <returns>Test data with exception types and messages</returns>
    public static IEnumerable<object[]> ExceptionPropagationScenarios()
    {
        yield return new object[] { typeof(InvalidOperationException), "Database connection failed" };
        yield return new object[] { typeof(TimeoutException), "Query timeout" };
        yield return new object[] { typeof(ArgumentException), "Invalid parameter" };
        yield return new object[] { typeof(Exception), "Unexpected error" };
    }

    /// <summary>
    /// Generates test data for concurrent request scenarios.
    /// </summary>
    /// <returns>Test data with request counts and concurrency levels</returns>
    public static IEnumerable<object[]> ConcurrentRequestScenarios()
    {
        yield return new object[] { 1, 1 };    // Single request
        yield return new object[] { 10, 5 };   // Multiple sequential
        yield return new object[] { 100, 10 }; // High volume
        yield return new object[] { 1000, 100 }; // Stress test
    }

    /// <summary>
    /// Generates test data for streaming request counts.
    /// </summary>
    /// <returns>Test data with expected item counts for streaming operations</returns>
    public static IEnumerable<object[]> StreamingRequestCounts()
    {
        yield return new object[] { 0 };
        yield return new object[] { 1 };
        yield return new object[] { 10 };
        yield return new object[] { 100 };
        yield return new object[] { 1000 };
    }

    /// <summary>
    /// Generates timeout scenarios for testing async operations.
    /// </summary>
    /// <returns>Test data with timeout values in milliseconds</returns>
    public static IEnumerable<object[]> TimeoutScenarios()
    {
        yield return new object[] { 100 };   // Very short
        yield return new object[] { 1000 };  // 1 second
        yield return new object[] { 5000 };  // 5 seconds
        yield return new object[] { 10000 }; // 10 seconds
    }
}
