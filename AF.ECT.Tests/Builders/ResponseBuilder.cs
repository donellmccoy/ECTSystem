namespace AF.ECT.Tests.Builders;

/// <summary>
/// Builder for creating test gRPC response objects with fluent interface.
/// Provides convenient methods for creating common response types with sensible defaults.
/// Reduces repetitive response construction and mock setup throughout unit tests.
/// </summary>
public class ResponseBuilder : BuilderBase<ResponseBuilder, object>
{
    #region Fields

    private object? _response;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new ResponseBuilder with default values.
    /// </summary>
    public static ResponseBuilder Create() => new();

    /// <summary>
    /// Creates a builder for GetReinvestigationRequestsResponse.
    /// </summary>
    public static ResponseBuilder CreateReinvestigationResponse(int itemCount = 1)
    {
        var builder = new ResponseBuilder();
        var response = new GetReinvestigationRequestsResponse();
        for (int i = 0; i < itemCount; i++)
        {
            response.Items.Add(new ReinvestigationRequestItem
            {
                Id = i + 1,
                Description = $"Test Request {i + 1}"
            });
        }
        builder._response = response;
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetUserNameResponse.
    /// </summary>
    public static ResponseBuilder CreateUserNameResponse(int itemCount = 1)
    {
        var builder = new ResponseBuilder();
        var response = new GetUserNameResponse();
        for (int i = 0; i < itemCount; i++)
        {
            response.Items.Add(new UserNameItem
            {
                UserId = i + 1,
                FirstName = "John",
                LastName = "Doe",
                FullName = "John Doe"
            });
        }
        builder._response = response;
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetManagedUsersResponse.
    /// </summary>
    public static ResponseBuilder CreateManagedUsersResponse(int itemCount = 1)
    {
        var builder = new ResponseBuilder();
        var response = new GetManagedUsersResponse();
        for (int i = 0; i < itemCount; i++)
        {
            response.Items.Add(new ManagedUserItem
            {
                UserId = i + 1,
                UserName = $"ManagedUser{i + 1}"
            });
        }
        builder._response = response;
        return builder;
    }

    /// <summary>
    /// Creates a builder for EmptyResponse (used by methods that return no data).
    /// </summary>
    public static ResponseBuilder CreateEmptyResponse()
    {
        var builder = new ResponseBuilder();
        builder._response = new EmptyRequest(); // Using EmptyRequest as workaround, adjust if better type exists
        return builder;
    }

    /// <summary>
    /// Creates a builder for response methods returning single value.
    /// </summary>
    public static ResponseBuilder CreateIntResponse(int value = 0)
    {
        var builder = new ResponseBuilder();
        // For int-returning methods, we'll use a generic response object
        builder._response = value;
        return builder;
    }

    /// <summary>
    /// Creates a builder for GetCancelReasonsResponse.
    /// </summary>
    public static ResponseBuilder CreateCancelReasonsResponse(int itemCount = 1)
    {
        var builder = new ResponseBuilder();
        var response = new GetCancelReasonsResponse();
        for (int i = 0; i < itemCount; i++)
        {
            response.Items.Add(new CancelReasonItem
            {
                ReasonId = i + 1,
                ReasonText = $"Cancel Reason {i + 1}",
                IsFormal = true
            });
        }
        builder._response = response;
        return builder;
    }

    #endregion

    #region Fluent Methods

    /// <summary>
    /// Sets a custom response object.
    /// </summary>
    public ResponseBuilder WithResponse<T>(T response) where T : class
    {
        _response = response;
        return Self;
    }

    /// <summary>
    /// Adds an item to a response collection (for responses with Items property).
    /// </summary>
    public ResponseBuilder AddItem<T>(T item) where T : class
    {
        if (_response is null)
        {
            throw new InvalidOperationException("Response was not initialized");
        }

        var itemsProperty = _response.GetType().GetProperty("Items");
        if (itemsProperty is null)
        {
            throw new InvalidOperationException(
                $"Response type {_response.GetType().Name} does not have an Items property");
        }

        var items = itemsProperty.GetValue(_response);
        if (items is not System.Collections.IList list)
        {
            throw new InvalidOperationException(
                $"Items property on {_response.GetType().Name} is not a list");
        }

        list.Add(item);
        return Self;
    }

    /// <summary>
    /// Modifies the current response using a mutator function.
    /// </summary>
    public ResponseBuilder Modify(Action<object?> mutator)
    {
        mutator(_response);
        return Self;
    }

    #endregion

    #region Build

    /// <summary>
    /// Builds and returns the response object.
    /// </summary>
    public override object Build() => _response ?? throw new InvalidOperationException("Response was not configured");

    /// <summary>
    /// Builds and returns the response object as a specific type.
    /// </summary>
    /// <typeparam name="T">The response type</typeparam>
    public T Build<T>() where T : class
    {
        if (_response is not T typed)
        {
            throw new InvalidCastException(
                $"Response is of type {_response?.GetType().Name ?? "null"}, " +
                $"but {typeof(T).Name} was requested");
        }
        return typed;
    }

    #endregion
}
