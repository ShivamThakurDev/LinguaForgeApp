namespace LinguaForge.Application.DTOs
{
    public class RecommendationDto
    {
        public string Topic { get; set; } = string.Empty;
        public string LessonKey { get; set; } = string.Empty;
        public string SuggestedExerciseType { get; set; } = "mcq";
        public string Level { get; set; } = "A1";
        public string Reason { get; set; } = string.Empty;
        public int PriorityScore { get; set; }
    }
}
