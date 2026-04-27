namespace LinguaForge.Application.DTOs
{
    public class PhonemeScoreDto
    {
        public string Phoneme { get; set; } = string.Empty;
        public double AccuracyScore { get; set; }
    }
}