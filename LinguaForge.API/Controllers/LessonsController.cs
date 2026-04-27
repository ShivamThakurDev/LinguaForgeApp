using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Mvc;

namespace LinguaForge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LessonsController : ControllerBase
    {
        private readonly LessonAppService _lessonAppService;

        public LessonsController(LessonAppService lessonAppService)
        {
            _lessonAppService = lessonAppService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<LessonDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLessons([FromQuery] string level = "A1", CancellationToken cancellationToken = default)
        {
            var lessons = await _lessonAppService.GetLessonsAsync(level, cancellationToken);
            return Ok(lessons);
        }
    }
}