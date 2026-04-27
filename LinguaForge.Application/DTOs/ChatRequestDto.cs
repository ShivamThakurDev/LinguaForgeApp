namespace LinguaForge.Application.DTOs
{
    public class ChatRequestDto
    {
        public List<ChatMessageDto> ConversationHistory { get; set; } = new();
    }
}