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
    public class RecommendationsController : ControllerBase
    {
        private readonly RecommendationAppService _recommendationAppService;

        public RecommendationsController(RecommendationAppService recommendationAppService)
        {
            _recommendationAppService = recommendationAppService;
        }

        [HttpGet("next")]
        [ProducesResponseType(typeof(RecommendationDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Next(CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return Unauthorized();
            }

            var recommendation = await _recommendationAppService.GetNextAsync(userId, cancellationToken);
            return Ok(recommendation);
        }
    }
}
