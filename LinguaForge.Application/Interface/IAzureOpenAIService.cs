using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface IAzureOpenAIService
    {
        Task<QuizExerciseDto> GenerateExerciseAsync(string topic, string level, string exerciseType, CancellationToken cancellationToken = default);
        Task<QuizEvaluationResponseDto> EvaluateExerciseAsync(QuizEvaluationRequestDto request, CancellationToken cancellationToken = default);
        Task<string> GetChatResponseAsync(IReadOnlyList<ChatMessageDto> conversationHistory, CancellationToken cancellationToken = default);
    }
}
