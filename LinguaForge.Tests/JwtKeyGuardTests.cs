using System;
using System.Security.Cryptography;
using LinguaForge.Infrastructure.Configuration;
using Xunit;

namespace LinguaForge.Tests;

public class JwtKeyGuardTests
{
    [Theory]
    [InlineData(null)]                                                   // missing
    [InlineData("")]                                                    // empty
    [InlineData("   ")]                                                 // whitespace
    [InlineData("too-short")]                                          // < 32 bytes
    [InlineData("CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_BYTES")] // appsettings.example placeholder
    [InlineData("local-dev-only-signing-key-change-me-32b+")]           // former docker-compose default (regression)
    [InlineData("LOCAL-DEV-ONLY-SIGNING-KEY-1234567890-abc")]           // placeholder variant, casing/separators
    public void Rejects_weak_or_placeholder_keys(string? key)
    {
        Assert.Throws<InvalidOperationException>(() => JwtKeyGuard.Validate(key));
    }

    [Fact]
    public void Accepts_a_strong_random_key()
    {
        var strong = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        var ex = Record.Exception(() => JwtKeyGuard.Validate(strong));

        Assert.Null(ex);
    }

    [Fact]
    public void Rejects_a_32_char_string_that_is_a_placeholder_even_though_long_enough()
    {
        // Long enough to pass the length check, but still an obvious placeholder.
        Assert.Throws<InvalidOperationException>(
            () => JwtKeyGuard.Validate("please-replace-me-with-a-real-secret"));
    }
}
