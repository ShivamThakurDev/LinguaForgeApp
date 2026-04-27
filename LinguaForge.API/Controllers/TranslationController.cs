using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Mvc;

namespace LinguaForge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TranslationController : ControllerBase
    {
        private readonly TranslationAppService _service;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(
            TranslationAppService service,
            ILogger<TranslationController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("Translate")]
        [ProducesResponseType(typeof(TranslateResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Translate([FromBody] TranslateRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _service.TranslateAsync(request);
                return Ok(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Azure Translator API call failed");
                return StatusCode(502, new { error = "Translation service unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during translation");
                return StatusCode(500, new { error = "An unexpected error occurred" });
            }
        }
        // Add this endpoint to TranslationController.cs

        [HttpGet("languages")]
        [ProducesResponseType(typeof(Dictionary<string, string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLanguages()
        {
            try
            {
                // Route through the app service or call directly — 
                // this is read-only infrastructure, no business logic needed
                var languages = await _service.GetSupportedLanguagesAsync();
                return Ok(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch supported languages");
                return StatusCode(502, new { error = "Could not retrieve language list" });
            }
        }
    }
}
