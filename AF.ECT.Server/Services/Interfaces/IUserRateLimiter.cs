namespace AF.ECT.Server.Services.Interfaces;

/// <summary>
/// Provides per-user rate limiting beyond IP-based limiting.
/// </summary>
/// <remarks>
/// Implements user-based quotas to prevent single authenticated users from overwhelming the system
/// while allowing multiple legitimate users to operate concurrently.
/// </remarks>
public interface IUserRateLimiter
{
    /// <summary>
    /// Checks if a user has exceeded their rate limit.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user is within their quota, false if rate limit exceeded.</returns>
    bool IsAllowed(string userId);

    /// <summary>
    /// Asynchronously checks if a user has exceeded their rate limit.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="maxRequestsPerMinute">Maximum requests allowed per minute for this user.</param>
    /// <returns>A task representing the asynchronous operation with result indicating if allowed.</returns>
    Task<bool> IsAllowedAsync(string userId, int maxRequestsPerMinute = 100);

    /// <summary>
    /// Resets the rate limit counter for a user.
    /// </summary>
    /// <param name="userId">The user ID to reset.</param>
    void ResetLimit(string userId);
}
