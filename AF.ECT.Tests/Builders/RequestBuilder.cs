namespace AF.ECT.Tests.Builders;

/// <summary>
/// Builder for creating test gRPC request objects with fluent interface.
/// Provides convenient methods for creating common request types with sensible defaults.
/// Reduces repetitive request construction throughout unit tests.
/// </summary>
public class RequestBuilder : BuilderBase<RequestBuilder, object>
{
    #region Fields

    private object? _request;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new RequestBuilder with default values.
    /// </summary>
    /// <returns>A new RequestBuilder instance</returns>
    public static RequestBuilder Create() => new();

    /// <summary>
    /// Creates a builder for GetReinvestigationRequestsRequest.
    /// </summary>
    public static RequestBuilder CreateReinvestigationRequest(int? userId = 1, bool? sarc = true)
    {
        var builder = new RequestBuilder();
        builder._request = new GetReinvestigationRequestsRequest
        {
            UserId = userId ?? 1,
            Sarc = sarc ?? true
        };
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetUserNameRequest.
    /// </summary>
    public static RequestBuilder CreateUserNameRequest(string? firstName = "John", string? lastName = "Doe")
    {
        var builder = new RequestBuilder();
        builder._request = new GetUserNameRequest
        {
            First = firstName,
            Last = lastName
        };
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetManagedUsersRequest.
    /// </summary>
    public static RequestBuilder CreateManagedUsersRequest(
        int? userId = 1,
        string? ssn = "123456789",
        string? name = "John Doe",
        int? status = 1,
        int? role = 1,
        int? srchUnit = 1,
        bool? showAllUsers = true)
    {
        var builder = new RequestBuilder();
        builder._request = new GetManagedUsersRequest
        {
            Userid = userId ?? 1,
            Ssn = ssn ?? "123456789",
            Name = name ?? "John Doe",
            Status = status ?? 1,
            Role = role ?? 1,
            SrchUnit = srchUnit ?? 1,
            ShowAllUsers = showAllUsers ?? true
        };
        return builder;
    }

    /// <summary>
    /// Creates a builder for SearchMemberDataRequest.
    /// </summary>
    public static RequestBuilder CreateSearchMemberDataRequest(string? lastName = "Doe", string? firstName = null)
    {
        var builder = new RequestBuilder();
        builder._request = new SearchMemberDataRequest
        {
            LastName = lastName,
            FirstName = firstName
        };
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetCancelReasonsRequest.
    /// </summary>
    public static RequestBuilder CreateCancelReasonsRequest(byte? workflowId = 1, bool? isFormal = true)
    {
        var builder = new RequestBuilder();
        builder._request = new GetCancelReasonsRequest
        {
            WorkflowId = workflowId ?? 1,
            IsFormal = isFormal ?? true
        };
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetWorkflowTitleRequest.
    /// </summary>
    public static RequestBuilder CreateWorkflowTitleRequest(int? moduleId = 1, int? subCase = 0)
    {
        var builder = new RequestBuilder();
        builder._request = new GetWorkflowTitleRequest
        {
            ModuleId = moduleId ?? 1,
            SubCase = subCase ?? 0
        };
        return builder;
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Sets a custom request object.
    /// </summary>
    public RequestBuilder WithRequest<T>(T request) where T : class
    {
        _request = request;
        return Self;
    }

    /// <summary>
    /// Modifies the current request using a mutator function.
    /// </summary>
    public RequestBuilder Modify(Action<object?> mutator)
    {
        mutator(_request);
        return Self;
    }

    #endregion

    #region Build

    /// <summary>
    /// Builds and returns the request object.
    /// </summary>
    /// <returns>The constructed request</returns>
    public override object Build() => _request ?? throw new InvalidOperationException("Request was not configured");

    /// <summary>
    /// Builds and returns the request object as a specific type.
    /// </summary>
    /// <typeparam name="T">The request type</typeparam>
    /// <returns>The constructed request cast to the specified type</returns>
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
