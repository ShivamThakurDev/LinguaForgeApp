using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using LinguaForge.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace LinguaForge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpeechController : ControllerBase
    {
        private readonly SpeechAppService _speechAppService;

        public SpeechController(SpeechAppService speechAppService)
        {
            _speechAppService = speechAppService;
        }

        [HttpPost("assess")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(15_000_000)]
        [ProducesResponseType(typeof(PronunciationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Assess([FromForm] SpeechAssessFormRequest request, CancellationToken cancellationToken = default)
        {
            if (request.Audio is null || request.Audio.Length == 0)
            {
                return BadRequest(new { error = "WAV audio file is required." });
            }

            if (string.IsNullOrWhiteSpace(request.ReferenceText))
            {
                return BadRequest(new { error = "referenceText is required." });
            }

            await using var ms = new MemoryStream();
            await request.Audio.CopyToAsync(ms, cancellationToken);

            var result = await _speechAppService.AssessAsync(ms.ToArray(), new SpeechAssessmentRequestDto
            {
                ReferenceText = request.ReferenceText,
                Locale = request.Locale
            }, cancellationToken);

            return Ok(result);
        }
    }
}
