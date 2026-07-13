namespace LinguaForge.Application.DTOs
{
    /// <summary>
    /// The client only names the lesson it finished. Identity comes from the JWT and
    /// XP/accuracy/title are computed server-side, so none of those are accepted here.
    /// </summary>
    public class CompleteLessonRequestDto
    {
        public string LessonKey { get; set; } = string.Empty;
    }
}
