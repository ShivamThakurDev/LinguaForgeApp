namespace LinguaForge.Application.DTOs
{
    public class GenerateExerciseRequestDto
    {
        public string Topic { get; set; } = "articles";
        public string Level { get; set; } = "A1";
        public string ExerciseType { get; set; } = "mcq";
    }
}