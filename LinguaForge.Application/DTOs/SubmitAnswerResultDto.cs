namespace LinguaForge.Application.DTOs
{
    public class SubmitAnswerResultDto
    {
        public bool IsCorrect { get; set; }
        public int EarnedXp { get; set; }
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }
}
