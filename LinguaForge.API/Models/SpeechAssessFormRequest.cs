using Microsoft.AspNetCore.Http;

namespace LinguaForge.API.Models
{
    public class SpeechAssessFormRequest
    {
        public IFormFile? Audio { get; set; }
        public string ReferenceText { get; set; } = string.Empty;
        public string Locale { get; set; } = "de-DE";
    }
}
