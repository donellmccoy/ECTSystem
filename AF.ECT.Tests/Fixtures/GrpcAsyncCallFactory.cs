namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Factory for creating mock AsyncUnaryCall instances for gRPC testing.
/// Simplifies setup of complex gRPC mock returns and eliminates boilerplate AsyncUnaryCall construction.
/// Enables cleaner, more readable gRPC client test setup.
/// </summary>
public static class GrpcAsyncCallFactory
{
    /// <summary>
    /// Creates an AsyncUnaryCall mock for a successful response.
    /// </summary>
    /// <typeparam name="T">The response type</typeparam>
    /// <param name="response">The response value to return</param>
    /// <returns>An AsyncUnaryCall configured to return the response</returns>
    public static AsyncUnaryCall<T> CreateSuccessCall<T>(T response)
        where T : class
    {
        return new AsyncUnaryCall<T>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => [],
            () => { });
    }

    /// <summary>
    /// Creates an AsyncUnaryCall mock for a failed RPC call.
    /// </summary>
    /// <typeparam name="T">The response type</typeparam>
    /// <param name="statusCode">The gRPC status code for the failure</param>
    /// <param name="statusDetail">Optional detail message about the failure</param>
    /// <returns>An AsyncUnaryCall configured to fail with the specified status</returns>
    public static AsyncUnaryCall<T> CreateFailureCall<T>(
        StatusCode statusCode,
        string statusDetail = "Test failure")
        where T : class
    {
        var status = new Status(statusCode, statusDetail);
        var exception = new RpcException(status);

        return new AsyncUnaryCall<T>(
            Task.FromException<T>(exception),
            Task.FromResult(new Metadata()),
            () => status,
            () => [],
            () => { });
    }

    /// <summary>
    /// Creates an AsyncUnaryCall mock that times out (DeadlineExceeded).
    /// </summary>
    /// <typeparam name="T">The response type</typeparam>
    /// <returns>An AsyncUnaryCall configured to timeout</returns>
    public static AsyncUnaryCall<T> CreateTimeoutCall<T>()
        where T : class
    {
        return CreateFailureCall<T>(StatusCode.DeadlineExceeded, "Call deadline exceeded");
    }

    /// <summary>
    /// Creates an AsyncUnaryCall mock that returns a default instance of the response type.
    /// Useful for methods that should return empty/default responses.
    /// </summary>
    /// <typeparam name="T">The response type</typeparam>
    /// <returns>An AsyncUnaryCall configured to return a default instance</returns>
    public static AsyncUnaryCall<T> CreateDefaultCall<T>()
        where T : class, new()
    {
        return CreateSuccessCall(new T());
    }
}
