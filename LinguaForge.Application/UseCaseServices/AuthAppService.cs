using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class AuthAppService
    {
        private readonly IAuthService _authService;

        public AuthAppService(IAuthService authService)
        {
            _authService = authService;
        }

        public Task<AuthResponseDto> RegisterAsync(AuthRegisterRequestDto request, CancellationToken cancellationToken = default)
            => _authService.RegisterAsync(request, cancellationToken);

        public Task<AuthResponseDto> LoginAsync(AuthLoginRequestDto request, CancellationToken cancellationToken = default)
            => _authService.LoginAsync(request, cancellationToken);

        public Task<AuthUserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
            => _authService.GetUserAsync(userId, cancellationToken);
    }
}
