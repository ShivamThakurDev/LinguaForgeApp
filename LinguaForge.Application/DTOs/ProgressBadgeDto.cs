namespace LinguaForge.Application.DTOs
{
    public class ProgressBadgeDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime UnlockedAtUtc { get; set; }
    }
}