namespace LinguaForge.Domain.Entities
{
    public class QuizAttempt
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string LessonKey { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string ExerciseType { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string SubmittedAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public int ScorePercent { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
    }
}
