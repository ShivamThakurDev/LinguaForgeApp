using System;
using LinguaForge.Infrastructure.Configuration;
using LinguaForge.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace LinguaForge.Tests;

public class LoginThrottleTests
{
    private static LoginThrottle Create(int permit = 3, int window = 60, int lockout = 300)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new LoginThrottleOptions
        {
            PermitLimit = permit,
            WindowSeconds = window,
            LockoutSeconds = lockout
        });
        return new LoginThrottle(cache, options);
    }

    [Fact]
    public void Locks_out_after_the_permit_limit_is_reached()
    {
        var throttle = Create(permit: 3);

        Assert.False(throttle.IsLocked("k", out _));
        throttle.RegisterFailure("k");
        throttle.RegisterFailure("k");
        Assert.False(throttle.IsLocked("k", out _)); // 2 failures, still under the limit

        throttle.RegisterFailure("k");               // 3rd failure trips the lockout

        Assert.True(throttle.IsLocked("k", out var retryAfter));
        Assert.True(retryAfter > TimeSpan.Zero);
        Assert.True(retryAfter <= TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void Success_clears_the_failure_counter()
    {
        var throttle = Create(permit: 3);
        throttle.RegisterFailure("k");
        throttle.RegisterFailure("k");

        throttle.RegisterSuccess("k");

        // Counter reset: two fresh failures must not immediately lock out.
        throttle.RegisterFailure("k");
        throttle.RegisterFailure("k");
        Assert.False(throttle.IsLocked("k", out _));
    }

    [Fact]
    public void Different_keys_are_independent()
    {
        var throttle = Create(permit: 3);
        for (var i = 0; i < 3; i++)
        {
            throttle.RegisterFailure("a");
        }

        Assert.True(throttle.IsLocked("a", out _));
        Assert.False(throttle.IsLocked("b", out _));
    }
}
