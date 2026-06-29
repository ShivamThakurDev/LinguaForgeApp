using LinguaForge.Application.DTOs;
using LinguaForge.Infrastructure.Configuration;
using LinguaForge.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace LinguaForge.Tests;

public class AuthServiceTests
{
    private static AuthService CreateService(SqliteTestContext ctx)
    {
        var jwt = Options.Create(new JwtOptions
        {
            Key = "test-signing-key-that-is-at-least-32-bytes-long",
            Issuer = "LinguaForge",
            Audience = "LinguaForge",
            ExpiryMinutes = 60
        });
        return new AuthService(ctx.Db, jwt);
    }

    [Fact]
    public async Task Register_then_login_succeeds_and_issues_token()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        var register = await service.RegisterAsync(new AuthRegisterRequestDto
        {
            Email = "Learner@Example.com",
            Password = "s3cret-pass",
            UserName = "Learner"
        });

        Assert.False(string.IsNullOrWhiteSpace(register.Token));
        Assert.Equal("learner@example.com", register.User.Email); // normalized

        var login = await service.LoginAsync(new AuthLoginRequestDto
        {
            Email = "learner@example.com",
            Password = "s3cret-pass"
        });

        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.Equal(register.User.Id, login.User.Id);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_rejected()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        await service.RegisterAsync(new AuthRegisterRequestDto
        {
            Email = "a@b.com",
            Password = "correct-horse",
            UserName = "A"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.LoginAsync(new AuthLoginRequestDto { Email = "a@b.com", Password = "wrong" }));
    }

    [Fact]
    public async Task Register_with_duplicate_email_throws()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        var first = new AuthRegisterRequestDto { Email = "dup@b.com", Password = "pw123456", UserName = "One" };
        await service.RegisterAsync(first);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterAsync(new AuthRegisterRequestDto { Email = "dup@b.com", Password = "pw123456", UserName = "Two" }));
    }

    [Fact]
    public async Task Register_issues_a_refresh_token_stored_hashed()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        var response = await service.RegisterAsync(new AuthRegisterRequestDto
        {
            Email = "r@b.com", Password = "pw123456", UserName = "R"
        });

        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        var stored = await ctx.Db.RefreshTokens.SingleAsync();
        Assert.NotEqual(response.RefreshToken, stored.TokenHash); // raw value is never stored
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task Refresh_rotates_and_revokes_the_old_token()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        var reg = await service.RegisterAsync(new AuthRegisterRequestDto { Email = "a@b.com", Password = "pw123456", UserName = "A" });
        var rotated = await service.RefreshAsync(reg.RefreshToken);

        Assert.NotEqual(reg.RefreshToken, rotated.RefreshToken);
        Assert.False(string.IsNullOrWhiteSpace(rotated.Token));
        Assert.Equal(2, await ctx.Db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task Reusing_a_rotated_token_revokes_the_whole_chain()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        var reg = await service.RegisterAsync(new AuthRegisterRequestDto { Email = "a@b.com", Password = "pw123456", UserName = "A" });
        var rotated = await service.RefreshAsync(reg.RefreshToken);

        // Replaying the already-rotated (revoked) token is treated as theft.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync(reg.RefreshToken));

        // The current token is now revoked too, so it can no longer be used.
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync(rotated.RefreshToken));
    }

    [Fact]
    public async Task Logout_revokes_the_refresh_token()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        var reg = await service.RegisterAsync(new AuthRegisterRequestDto { Email = "a@b.com", Password = "pw123456", UserName = "A" });
        await service.LogoutAsync(reg.RefreshToken);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync(reg.RefreshToken));
    }

    [Fact]
    public async Task Refresh_with_unknown_token_is_rejected()
    {
        using var ctx = new SqliteTestContext();
        var service = CreateService(ctx);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.RefreshAsync("not-a-real-token"));
    }
}
