using LinguaForge.Application.Interface;
using LinguaForge.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LinguaForge.Infrastructure.Services
{
    /// <summary>
    /// In-memory login throttle: counts failed attempts per opaque key (IP + email) inside a
    /// rolling window and, once <see cref="LoginThrottleOptions.PermitLimit"/> is reached, locks
    /// that key out for <see cref="LoginThrottleOptions.LockoutSeconds"/>. Backed by
    /// <see cref="IMemoryCache"/> so entries self-expire; a single gate keeps counter mutations
    /// atomic (login is low-throughput, so a global lock is fine). (LF-105)
    /// </summary>
    public sealed class LoginThrottle : ILoginThrottle
    {
        private readonly IMemoryCache _cache;
        private readonly LoginThrottleOptions _options;
        private readonly object _gate = new();

        public LoginThrottle(IMemoryCache cache, IOptions<LoginThrottleOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        public bool IsLocked(string key, out TimeSpan retryAfter)
        {
            retryAfter = TimeSpan.Zero;
            lock (_gate)
            {
                if (_cache.TryGetValue(CacheKey(key), out Entry? entry) && entry!.LockedUntilUtc is { } until)
                {
                    var remaining = until - DateTime.UtcNow;
                    if (remaining > TimeSpan.Zero)
                    {
                        retryAfter = remaining;
                        return true;
                    }
                }
            }
            return false;
        }

        public void RegisterFailure(string key)
        {
            lock (_gate)
            {
                var now = DateTime.UtcNow;
                var entry = _cache.GetOrCreate(CacheKey(key), cacheEntry =>
                {
                    // Keep the record alive long enough to span either the counting window or an
                    // active lockout, whichever is longer.
                    cacheEntry.SlidingExpiration =
                        TimeSpan.FromSeconds(Math.Max(_options.WindowSeconds, _options.LockoutSeconds) + 1);
                    return new Entry { WindowStartUtc = now };
                })!;

                // Once a lockout is active it stays until it expires; otherwise reset the counter
                // when the rolling window has elapsed.
                if (entry.LockedUntilUtc is null &&
                    now - entry.WindowStartUtc > TimeSpan.FromSeconds(_options.WindowSeconds))
                {
                    entry.WindowStartUtc = now;
                    entry.Failures = 0;
                }

                entry.Failures++;
                if (entry.Failures >= _options.PermitLimit)
                {
                    entry.LockedUntilUtc = now.AddSeconds(_options.LockoutSeconds);
                }
            }
        }

        public void RegisterSuccess(string key)
        {
            lock (_gate)
            {
                _cache.Remove(CacheKey(key));
            }
        }

        private static string CacheKey(string key) => $"login-throttle:{key}";

        private sealed class Entry
        {
            public int Failures;
            public DateTime WindowStartUtc;
            public DateTime? LockedUntilUtc;
        }
    }
}
