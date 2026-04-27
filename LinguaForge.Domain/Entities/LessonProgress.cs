namespace LinguaForge.Domain.Entities
{
    public class LessonProgress
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string LessonKey { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public int Attempts { get; set; }
        public int AccuracyPercent { get; set; }
        public int EarnedXp { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}