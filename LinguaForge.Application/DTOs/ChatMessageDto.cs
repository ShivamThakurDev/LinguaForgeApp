namespace LinguaForge.Application.DTOs
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }
}