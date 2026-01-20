using AF.ECT.Server.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace AF.ECT.Server.Services;

/// <summary>
/// Implements per-user rate limiting to prevent resource exhaustion by individual users.
/// </summary>
/// <remarks>
/// Tracks request counts per user and enforces configurable limits per minute.
/// Automatically resets counters after each minute window.
/// </remarks>
public class UserRateLimiter : IUserRateLimiter
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserRateLimiter> _logger;

    /// <summary>
    /// Initializes a new instance of the UserRateLimiter.
    /// </summary>
    /// <param name="cache">The memory cache for storing rate limit counters.</param>
    /// <param name="logger">The logger for monitoring rate limit violations.</param>
    public UserRateLimiter(IMemoryCache cache, ILogger<UserRateLimiter> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks if a user has exceeded their rate limit.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <returns>True if the user is within their quota, false if rate limit exceeded.</returns>
    public bool IsAllowed(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return true; // Allow unauthenticated requests
        }

        var key = $"user_rate_limit:{userId}";
        var currentCount = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });

        if (currentCount >= 100)
        {
            _logger.LogWarning("User {UserId} exceeded rate limit ({Count}/100 requests)", userId, currentCount);
            return false;
        }

        _cache.Set(key, currentCount + 1, TimeSpan.FromMinutes(1));
        return true;
    }

    /// <summary>
    /// Asynchronously checks if a user has exceeded their rate limit.
    /// </summary>
    /// <param name="userId">The user ID to check.</param>
    /// <param name="maxRequestsPerMinute">Maximum requests allowed per minute for this user.</param>
    /// <returns>A task representing the asynchronous operation with result indicating if allowed.</returns>
    public async Task<bool> IsAllowedAsync(string userId, int maxRequestsPerMinute = 100)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return await Task.FromResult(true); // Allow unauthenticated requests
        }

        var key = $"user_rate_limit:{userId}";
        var currentCount = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
            return 0;
        });

        if (currentCount >= maxRequestsPerMinute)
        {
            _logger.LogWarning("User {UserId} exceeded rate limit ({Count}/{Max} requests)", userId, currentCount, maxRequestsPerMinute);
            return await Task.FromResult(false);
        }

        _cache.Set(key, currentCount + 1, TimeSpan.FromMinutes(1));
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Resets the rate limit counter for a user.
    /// </summary>
    /// <param name="userId">The user ID to reset.</param>
    public void ResetLimit(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var key = $"user_rate_limit:{userId}";
        _cache.Remove(key);
        _logger.LogInformation("Rate limit reset for user {UserId}", userId);
    }
}
