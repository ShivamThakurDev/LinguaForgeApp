namespace LinguaForge.Application.DTOs
{
    public class QuizExerciseDto
    {
        public string ExerciseType { get; set; } = "mcq";
        public string Question { get; set; } = string.Empty;
        public List<QuizOptionDto> Options { get; set; } = new();
        public string CorrectOptionId { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
    }
}