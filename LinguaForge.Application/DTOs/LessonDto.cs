namespace LinguaForge.Application.DTOs
{
    public class LessonDto
    {
        public string LessonKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Level { get; set; } = "A1";
        public string Description { get; set; } = string.Empty;
        public IReadOnlyList<LessonVocabDto> Vocabulary { get; set; } = Array.Empty<LessonVocabDto>();
    }
}