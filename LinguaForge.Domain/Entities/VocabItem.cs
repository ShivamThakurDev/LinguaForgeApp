namespace LinguaForge.Domain.Entities
{
    public class VocabItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string LessonKey { get; set; } = string.Empty;
        public string German { get; set; } = string.Empty;
        public string English { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;
        public string CefrLevel { get; set; } = "A1";
        public string? AudioUrl { get; set; }
    }
}