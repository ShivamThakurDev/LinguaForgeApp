using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinguaForge.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserProgressAppService _progressService;

        public UserController(UserProgressAppService progressService)
        {
            _progressService = progressService;
        }

        [HttpGet("progress")]
        [ProducesResponseType(typeof(UserProgressDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProgress(CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var progress = await _progressService.GetProgressAsync(userId, cancellationToken);
            return Ok(progress);
        }

        [HttpPost("progress/lesson-complete")]
        [ProducesResponseType(typeof(UserProgressDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CompleteLesson([FromBody] CompleteLessonRequestDto request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(request.LessonKey))
            {
                return BadRequest(new { error = "lessonKey is required." });
            }

            var progress = await _progressService.RecordCompletionAsync(userId, request.LessonKey, cancellationToken);
            return Ok(progress);
        }
    }
}
