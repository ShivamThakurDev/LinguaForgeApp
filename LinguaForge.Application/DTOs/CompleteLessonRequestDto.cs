namespace LinguaForge.Application.DTOs
{
    public class CompleteLessonRequestDto
    {
        public Guid UserId { get; set; }
        public string LessonKey { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public int AccuracyPercent { get; set; }
        public int EarnedXp { get; set; } = 10;
    }
}