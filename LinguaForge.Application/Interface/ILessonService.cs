using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface ILessonService
    {
        Task<IReadOnlyList<LessonDto>> GetLessonsAsync(string level, CancellationToken cancellationToken = default);

        /// <summary>
        /// Grades a submitted answer against the stored correct answer (server-side) and
        /// records the attempt. Returns null if the exercise does not exist.
        /// </summary>
        Task<SubmitAnswerResultDto?> EvaluateAnswerAsync(Guid userId, SubmitAnswerRequestDto request, CancellationToken cancellationToken = default);
    }
}
