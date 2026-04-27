using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class UserProgressAppService
    {
        private readonly IUserProgressService _userProgressService;

        public UserProgressAppService(IUserProgressService userProgressService)
        {
            _userProgressService = userProgressService;
        }

        public Task<UserProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default)
            => _userProgressService.GetProgressAsync(userId, cancellationToken);

        public Task<UserProgressDto> RecordCompletionAsync(CompleteLessonRequestDto request, CancellationToken cancellationToken = default)
            => _userProgressService.RecordLessonCompletionAsync(request, cancellationToken);
    }
}