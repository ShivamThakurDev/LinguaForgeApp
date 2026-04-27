namespace LinguaForge.Application.DTOs
{
    public class QuizEvaluationResponseDto
    {
        public bool IsCorrect { get; set; }
        public int ScorePercent { get; set; }
        public int EarnedXp { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public string CorrectedAnswer { get; set; } = string.Empty;
        public string WeakTopic { get; set; } = string.Empty;
    }
}
