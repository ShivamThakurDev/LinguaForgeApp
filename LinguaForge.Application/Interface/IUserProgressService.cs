using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface IUserProgressService
    {
        Task<UserProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<UserProgressDto> RecordLessonCompletionAsync(CompleteLessonRequestDto request, CancellationToken cancellationToken = default);
        Task<UserProgressDto> AwardQuizXpAsync(Guid userId, int earnedXp, CancellationToken cancellationToken = default);
    }
}
