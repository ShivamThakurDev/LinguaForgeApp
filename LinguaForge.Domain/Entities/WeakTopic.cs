namespace LinguaForge.Domain.Entities
{
    public class WeakTopic
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string TopicCode { get; set; } = string.Empty;
        public int MistakeCount { get; set; }
        public DateTime? LastMistakeAtUtc { get; set; }
        public DateTime LastUpdatedAtUtc { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
