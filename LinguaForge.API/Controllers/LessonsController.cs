using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        // Anonymous on purpose: lesson content powers the guest trial experience.
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<LessonDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLessons([FromQuery] string level = "A1", CancellationToken cancellationToken = default)
        {
            var lessons = await _lessonAppService.GetLessonsAsync(level, cancellationToken);
            return Ok(lessons);
        }

        // Server-authoritative scoring: the correct answer is looked up on the server,
        // never trusted from the client. Requires a signed-in learner.
        [Authorize]
        [HttpPost("answer")]
        [ProducesResponseType(typeof(SubmitAnswerResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequestDto request, CancellationToken cancellationToken)
        {
            if (request.ExerciseId == Guid.Empty || string.IsNullOrWhiteSpace(request.SubmittedAnswer))
            {
                return BadRequest(new { error = "exerciseId and submittedAnswer are required." });
            }

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var result = await _lessonAppService.EvaluateAnswerAsync(userId, request, cancellationToken);
            return result is null ? NotFound(new { error = "Exercise not found." }) : Ok(result);
        }
    }
}