namespace LinguaForge.Domain.Entities
{
    /// <summary>
    /// A lesson within a CEFR level. Identified by a stable LessonKey (e.g. "a1-greetings")
    /// which vocabulary and exercises reference. Lessons are authored as data (seeded from
    /// the course JSON), so adding a lesson never requires a code change.
    /// </summary>
    public class Lesson
    {
        public string LessonKey { get; set; } = string.Empty;
        public string Level { get; set; } = "A1";
        public int Order { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
