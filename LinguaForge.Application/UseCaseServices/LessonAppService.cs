using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class LessonAppService
    {
        private readonly ILessonService _lessonService;

        public LessonAppService(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        public Task<IReadOnlyList<LessonDto>> GetLessonsAsync(string level, CancellationToken cancellationToken = default)
            => _lessonService.GetLessonsAsync(level, cancellationToken);
    }
}