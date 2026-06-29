using LinguaForge.Application.DTOs;
using LinguaForge.Infrastructure.Configuration;
using LinguaForge.Infrastructure.Services;
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
}
