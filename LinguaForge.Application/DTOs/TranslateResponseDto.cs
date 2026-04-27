using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinguaForge.Application.DTOs
{

    public class TranslateResponseDto
    {
        public string TranslatedText { get; set; } = string.Empty;
        public string DetectedSourceLanguage { get; set; } = string.Empty;
        public string LanguagePair { get; set; } = string.Empty;  // e.g. "en→de"
        public int CharacterCount { get; set; }
    }
}
