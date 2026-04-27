namespace LinguaForge.Application.DTOs
{
    public class SpeechAssessmentRequestDto
    {
        public string ReferenceText { get; set; } = string.Empty;
        public string Locale { get; set; } = "de-DE";
    }
}