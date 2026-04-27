namespace LinguaForge.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalXp { get; set; }
        public int CurrentStreakDays { get; set; }
        public int Level { get; set; } = 1;
        public DateTime? LastLessonCompletedOnUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
        public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
        public ICollection<WeakTopic> WeakTopics { get; set; } = new List<WeakTopic>();
        public ICollection<UserBadge> UserBadges { get; set; } = new List<UserBadge>();
        public AuthCredential? AuthCredential { get; set; }
    }
}
