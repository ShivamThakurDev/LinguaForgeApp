using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface ILessonService
    {
        Task<IReadOnlyList<LessonDto>> GetLessonsAsync(string level, CancellationToken cancellationToken = default);
    }
}