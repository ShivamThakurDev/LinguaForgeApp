using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(AuthRegisterRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> LoginAsync(AuthLoginRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>Rotates a valid refresh token into a new access + refresh pair.</summary>
        Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>Revokes a refresh token (logout). Idempotent.</summary>
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
