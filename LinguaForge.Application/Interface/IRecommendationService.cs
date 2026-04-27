using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface IRecommendationService
    {
        Task RecordQuizOutcomeAsync(Guid userId, QuizEvaluationRequestDto request, QuizEvaluationResponseDto response, CancellationToken cancellationToken = default);
        Task<RecommendationDto> GetNextRecommendationAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
