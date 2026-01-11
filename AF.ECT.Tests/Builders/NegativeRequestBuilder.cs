namespace AF.ECT.Tests.Builders;

/// <summary>
/// Builder for creating invalid/boundary test requests to validate error handling.
/// Enables negative test scenarios without repeating manual invalid data construction.
/// </summary>
public class NegativeRequestBuilder : BuilderBase<NegativeRequestBuilder, object>
{
    #region Fields

    private object? _request;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new NegativeRequestBuilder.
    /// </summary>
    public static NegativeRequestBuilder Create() => new();

    /// <summary>
    /// Creates request with null or empty parameters.
    /// </summary>
    public static NegativeRequestBuilder CreateNullParameterRequest(string scenarioName)
    {
        var builder = new NegativeRequestBuilder();
        builder._request = scenarioName switch
        {
            "GetUserName_NullLastName" => new GetUserNameRequest
            {
                First = "John",
                Last = null  // Invalid: last name required
            },
            "GetUserName_EmptyBothNames" => new GetUserNameRequest
            {
                First = "",
                Last = ""  // Invalid: both empty
            },
            "SearchMemberData_NullLastName" => new SearchMemberDataRequest
            {
                LastName = null  // Invalid: required field
            },
            "GetManagedUsers_NegativeUserId" => new GetManagedUsersRequest
            {
                Userid = -1  // Invalid: negative ID
            },
            _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
        };
        return builder;
    }

    /// <summary>
    /// Creates request with boundary value violations.
    /// </summary>
    public static NegativeRequestBuilder CreateBoundaryViolationRequest(string scenarioName)
    {
        var builder = new NegativeRequestBuilder();
        builder._request = scenarioName switch
        {
            "GetManagedUsers_MaxIntUserId" => new GetManagedUsersRequest
            {
                Userid = int.MaxValue  // Boundary: extremely large ID
            },
            "GetManagedUsers_NegativeStatus" => new GetManagedUsersRequest
            {
                Status = -1  // Invalid: status cannot be negative
            },
            "GetCancelReasons_ZeroWorkflowId" => new GetCancelReasonsRequest
            {
                WorkflowId = 0  // Boundary: zero as ID
            },
            "GetCancelReasons_MaxByteWorkflowId" => new GetCancelReasonsRequest
            {
                WorkflowId = byte.MaxValue  // Boundary: maximum byte value
            },
            _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
        };
        return builder;
    }

    /// <summary>
    /// Creates request with oversized string parameters.
    /// </summary>
    public static NegativeRequestBuilder CreateOversizedStringRequest(string scenarioName)
    {
        var builder = new NegativeRequestBuilder();
        var hugeString = new string('x', 10000);  // 10k character string
        
        builder._request = scenarioName switch
        {
            "GetUserName_HugeLastName" => new GetUserNameRequest
            {
                First = "John",
                Last = hugeString  // Invalid: extremely long string
            },
            "SearchMemberData_HugeName" => new SearchMemberDataRequest
            {
                LastName = hugeString  // Invalid: oversized parameter
            },
            _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
        };
        return builder;
    }

    /// <summary>
    /// Creates request with special/invalid characters.
    /// </summary>
    public static NegativeRequestBuilder CreateInvalidCharacterRequest(string scenarioName)
    {
        var builder = new NegativeRequestBuilder();
        builder._request = scenarioName switch
        {
            "GetUserName_SQLInjectionAttempt" => new GetUserNameRequest
            {
                First = "'; DROP TABLE users; --",
                Last = "' OR '1'='1"  // SQL injection attempt
            },
            "SearchMemberData_XMLSpecialChars" => new SearchMemberDataRequest
            {
                LastName = "<script>alert('xss')</script>"  // XSS attempt
            },
            "GetUserName_ControlCharacters" => new GetUserNameRequest
            {
                First = "John\x00\x01\x02",  // Invalid: control characters
                Last = "Doe"
            },
            _ => throw new ArgumentException($"Unknown scenario: {scenarioName}")
        };
        return builder;
    }

    #endregion

    #region Build

    /// <summary>
    /// Builds and returns the invalid request object.
    /// </summary>
    public override object Build() => _request ?? throw new InvalidOperationException("Request was not configured");

    /// <summary>
    /// Builds and returns the request as a specific type.
    /// </summary>
    /// <typeparam name="T">The request type</typeparam>
    public T Build<T>() where T : class
    {
        if (_request is not T typed)
        {
            throw new InvalidCastException(
                $"Request is of type {_request?.GetType().Name ?? "null"}, " +
                $"but {typeof(T).Name} was requested");
        }
        return typed;
    }

    #endregion
}
