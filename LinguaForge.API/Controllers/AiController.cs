using LinguaForge.Application.DTOs;
using LinguaForge.Application.UseCaseServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LinguaForge.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [EnableRateLimiting("MeteredApi")]
    public class AiController : ControllerBase
    {
        private readonly AiChatAppService _chatService;

        public AiController(AiChatAppService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("chat")]
        [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Chat([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
        {
            if (request.ConversationHistory.Count == 0)
            {
                return BadRequest(new { error = "conversationHistory cannot be empty." });
            }

            var response = await _chatService.GetChatResponseAsync(request, cancellationToken);
            return Ok(response);
        }
    }
}