using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class QuizAppService
    {
        private readonly IAzureOpenAIService _openAiService;
        private readonly IRecommendationService _recommendationService;
        private readonly IUserProgressService _userProgressService;

        public QuizAppService(
            IAzureOpenAIService openAiService,
            IRecommendationService recommendationService,
            IUserProgressService userProgressService)
        {
            _openAiService = openAiService;
            _recommendationService = recommendationService;
            _userProgressService = userProgressService;
        }

        public async Task<GenerateExerciseResponseDto> GenerateExerciseAsync(GenerateExerciseRequestDto request, CancellationToken cancellationToken = default)
        {
            var exercise = await _openAiService.GenerateExerciseAsync(request.Topic, request.Level, request.ExerciseType, cancellationToken);
            return new GenerateExerciseResponseDto { Exercise = exercise };
        }

        public async Task<QuizEvaluationResponseDto> EvaluateExerciseAsync(Guid userId, QuizEvaluationRequestDto request, CancellationToken cancellationToken = default)
        {
            var result = await _openAiService.EvaluateExerciseAsync(request, cancellationToken);
            await _recommendationService.RecordQuizOutcomeAsync(userId, request, result, cancellationToken);

            if (result.EarnedXp > 0)
            {
                await _userProgressService.AwardQuizXpAsync(userId, result.EarnedXp, cancellationToken);
            }

            return result;
        }
    }
}
