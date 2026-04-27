namespace LinguaForge.Application.DTOs
{
    public class PronunciationResultDto
    {
        public double AccuracyScore { get; set; }
        public double FluencyScore { get; set; }
        public double CompletenessScore { get; set; }
        public double PronunciationScore { get; set; }
        public IReadOnlyList<PhonemeScoreDto> Phonemes { get; set; } = Array.Empty<PhonemeScoreDto>();
        public string RecognizedText { get; set; } = string.Empty;
    }
}