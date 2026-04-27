namespace LinguaForge.Application.DTOs
{
    public class QuizEvaluationRequestDto
    {
        public string LessonKey { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string Level { get; set; } = "A1";
        public string ExerciseType { get; set; } = "mcq";
        public string Question { get; set; } = string.Empty;
        public string PromptText { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public string SubmittedAnswer { get; set; } = string.Empty;
    }
}
