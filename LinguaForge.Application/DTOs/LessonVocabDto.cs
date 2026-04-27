namespace LinguaForge.Application.DTOs
{
    public class LessonVocabDto
    {
        public string German { get; set; } = string.Empty;
        public string English { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public string? AudioUrl { get; set; }
    }
}