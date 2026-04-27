using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LinguaForge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly QuizAppService _quizAppService;

        public QuizController(QuizAppService quizAppService)
        {
            _quizAppService = quizAppService;
        }

        [HttpPost("generate")]
        [ProducesResponseType(typeof(GenerateExerciseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Generate([FromBody] GenerateExerciseRequestDto request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _quizAppService.GenerateExerciseAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(QuizEvaluationResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Evaluate([FromBody] QuizEvaluationRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.SubmittedAnswer) || string.IsNullOrWhiteSpace(request.CorrectAnswer))
            {
                return BadRequest(new { error = "submittedAnswer and correctAnswer are required." });
            }

            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var result = await _quizAppService.EvaluateExerciseAsync(userId, request, cancellationToken);
            return Ok(result);
        }
    }
}
