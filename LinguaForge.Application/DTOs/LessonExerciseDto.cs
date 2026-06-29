namespace LinguaForge.Application.DTOs
{
    /// <summary>
    /// Client-safe view of an exercise. Deliberately omits the correct answer and
    /// explanation so the browser cannot reveal or score answers locally.
    /// </summary>
    public class LessonExerciseDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = "mcq";
        public string PromptText { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();
        public string Topic { get; set; } = string.Empty;
    }
}
