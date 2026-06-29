namespace LinguaForge.Application.DTOs
{
    public class SubmitAnswerRequestDto
    {
        public Guid ExerciseId { get; set; }
        public string SubmittedAnswer { get; set; } = string.Empty;
    }
}
