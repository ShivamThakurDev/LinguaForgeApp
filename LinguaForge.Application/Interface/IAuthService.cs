using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(AuthRegisterRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthResponseDto> LoginAsync(AuthLoginRequestDto request, CancellationToken cancellationToken = default);
        Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
