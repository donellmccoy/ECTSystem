namespace AF.ECT.Tests.Data;

using System.Collections.Generic;
using Xunit;

// ========== WorkflowServiceTestData Stubs ==========

/// <summary>
/// Test data for WorkflowService constructor null parameter validation.
/// </summary>
public class WorkflowServiceConstructorNullParameterData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { true, false };   // null logger, valid data service
        yield return new object[] { false, true };   // valid logger, null data service
        yield return new object[] { true, true };    // both null
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Test data for GetReinvestigationRequests with different request counts.
/// </summary>
public class WorkflowServiceRequestScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { 1 };      // Single item
        yield return new object[] { 5 };      // Small batch
        yield return new object[] { 100 };    // Medium batch
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class WorkflowServiceNameFormatData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class WorkflowServiceExceptionTypeData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ========== WorkflowClientTestData Stubs ==========

public class WorkflowClientNullableParameterData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class WorkflowClientLargeIntegerData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class WorkflowClientStringParameterData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class WorkflowClientWorkflowParameterData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ========== ResilienceServiceTestData Stubs ==========

public class RetryScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CircuitBreakerStateTransitionsData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DatabaseOperationScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class TimeoutScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ExponentialBackoffScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ========== DbContextExtensionsTestData Stubs ==========

public class DbContextExtensionsSqlQueryData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DbContextExtensionsParameterData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ========== DataServiceTestData Stubs ==========

/// <summary>
/// Stub test data for DataService tests.
/// These are placeholders - implement with actual test scenarios when needed.
/// </summary>
public class DataServiceParameterCombinationData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DataServiceExceptionTypeData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ========== ChaosTestData Stubs ==========

public class NetworkFailureScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class DatabaseFailureScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class MixedFailureScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class HighLoadScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class CircuitBreakerRecoveryScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class PerformanceUnderFailureScenariosData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator() => new List<object[]>().GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// ========== Export static helpers ==========

/// <summary>
/// Static exports for test data - allows "using static" imports in test files.
/// </summary>
public static class WorkflowServiceTestData { }
public static class WorkflowClientTestData { }
public static class ResilienceServiceTestData { }
public static class DbContextExtensionsTestData { }
public static class DataServiceTestData { }
public static class ChaosTestData { }
