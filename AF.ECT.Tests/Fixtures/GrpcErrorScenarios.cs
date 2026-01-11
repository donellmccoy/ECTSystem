namespace AF.ECT.Tests.Fixtures;

/// <summary>
/// Helper for testing gRPC error scenarios and exception handling.
/// Provides common error patterns for validating RPC exception handling across services.
/// </summary>
public static class GrpcErrorScenarios
{
    /// <summary>
    /// Creates an RpcException for a deadline exceeded scenario (timeout).
    /// </summary>
    public static RpcException CreateDeadlineExceededException(
        string? message = "Deadline exceeded before operation could complete")
    {
        return new RpcException(new Status(StatusCode.DeadlineExceeded, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for a cancelled operation scenario.
    /// </summary>
    public static RpcException CreateCancelledOperationException(
        string? message = "Operation was cancelled")
    {
        return new RpcException(new Status(StatusCode.Cancelled, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for authentication failures.
    /// </summary>
    public static RpcException CreateUnauthenticatedException(
        string? message = "Request lacks valid authentication credentials")
    {
        return new RpcException(new Status(StatusCode.Unauthenticated, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for permission/authorization failures.
    /// </summary>
    public static RpcException CreatePermissionDeniedException(
        string? message = "Caller does not have permission to execute the specified operation")
    {
        return new RpcException(new Status(StatusCode.PermissionDenied, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for resource not found scenarios.
    /// </summary>
    public static RpcException CreateNotFoundException(
        string? message = "The specified resource was not found")
    {
        return new RpcException(new Status(StatusCode.NotFound, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for internal server errors.
    /// </summary>
    public static RpcException CreateInternalException(
        string? message = "Internal server error")
    {
        return new RpcException(new Status(StatusCode.Internal, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for unavailable service scenario.
    /// </summary>
    public static RpcException CreateUnavailableException(
        string? message = "Service is currently unavailable")
    {
        return new RpcException(new Status(StatusCode.Unavailable, message ?? ""));
    }

    /// <summary>
    /// Creates an RpcException for invalid argument scenario.
    /// </summary>
    public static RpcException CreateInvalidArgumentException(
        string? message = "Client specified an invalid argument")
    {
        return new RpcException(new Status(StatusCode.InvalidArgument, message ?? ""));
    }

    /// <summary>
    /// Configures a mock gRPC client method to throw the specified RpcException.
    /// </summary>
    public static void SetupMockMethodToThrowException<TClient, TRequest, TResponse>(
        Mock<TClient> mockClient,
        Expression<Func<TClient, AsyncUnaryCall<TResponse>>> methodSelector,
        RpcException exception)
        where TClient : class
        where TRequest : class
        where TResponse : class
    {
        mockClient
            .Setup(methodSelector)
            .Returns(GrpcAsyncCallFactory.CreateFailureCall<TResponse>(exception.StatusCode, exception.Status.Detail));
    }

    /// <summary>
    /// Configures a mock gRPC client method to timeout (DeadlineExceeded).
    /// </summary>
    public static void SetupMockMethodToTimeout<TClient, TResponse>(
        Mock<TClient> mockClient,
        Expression<Func<TClient, AsyncUnaryCall<TResponse>>> methodSelector)
        where TClient : class
        where TResponse : class
    {
        mockClient
            .Setup(methodSelector)
            .Returns(GrpcAsyncCallFactory.CreateTimeoutCall<TResponse>());
    }

    /// <summary>
    /// Represents a gRPC error scenario for Theory-based testing.
    /// </summary>
    public class ErrorScenario
    {
        /// <summary>
        /// Gets the human-readable name of the scenario.
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// Gets the gRPC status code expected in this scenario.
        /// </summary>
        public StatusCode ExpectedStatusCode { get; init; }

        /// <summary>
        /// Gets the exception factory for this scenario.
        /// </summary>
        public Func<RpcException> ExceptionFactory { get; init; } = null!;
    }

    /// <summary>
    /// Gets common error scenarios for Theory-based negative testing.
    /// </summary>
    public static IEnumerable<ErrorScenario> CommonErrorScenarios => new[]
    {
        new ErrorScenario
        {
            Name = "Timeout (DeadlineExceeded)",
            ExpectedStatusCode = StatusCode.DeadlineExceeded,
            ExceptionFactory = () => CreateDeadlineExceededException()
        },
        new ErrorScenario
        {
            Name = "Cancelled Operation",
            ExpectedStatusCode = StatusCode.Cancelled,
            ExceptionFactory = () => CreateCancelledOperationException()
        },
        new ErrorScenario
        {
            Name = "Unauthenticated",
            ExpectedStatusCode = StatusCode.Unauthenticated,
            ExceptionFactory = () => CreateUnauthenticatedException()
        },
        new ErrorScenario
        {
            Name = "Permission Denied",
            ExpectedStatusCode = StatusCode.PermissionDenied,
            ExceptionFactory = () => CreatePermissionDeniedException()
        },
        new ErrorScenario
        {
            Name = "Not Found",
            ExpectedStatusCode = StatusCode.NotFound,
            ExceptionFactory = () => CreateNotFoundException()
        },
        new ErrorScenario
        {
            Name = "Internal Error",
            ExpectedStatusCode = StatusCode.Internal,
            ExceptionFactory = () => CreateInternalException()
        },
        new ErrorScenario
        {
            Name = "Unavailable",
            ExpectedStatusCode = StatusCode.Unavailable,
            ExceptionFactory = () => CreateUnavailableException()
        }
    };
}
