using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class AiChatAppService
    {
        private readonly IAzureOpenAIService _openAiService;

        public AiChatAppService(IAzureOpenAIService openAiService)
        {
            _openAiService = openAiService;
        }

        public async Task<ChatResponseDto> GetChatResponseAsync(ChatRequestDto request, CancellationToken cancellationToken = default)
        {
            var message = await _openAiService.GetChatResponseAsync(request.ConversationHistory, cancellationToken);
            return new ChatResponseDto { Message = message };
        }
    }
}