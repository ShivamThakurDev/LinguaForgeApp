using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinguaForge.Application.UseCaseServices
{
    public class TranslationAppService
    {
        private readonly ITranslationService _translationService;

        public TranslationAppService(ITranslationService translationService)
        {
            _translationService = translationService;
        }

        public async Task<TranslateResponseDto> TranslateAsync(TranslateRequestDto request)
        {
            // Auto-detect if 'from' is blank
            var detectedLang = string.IsNullOrWhiteSpace(request.From)
                ? await _translationService.DetectLanguageAsync(request.Text)
                : request.From;

            var translated = await _translationService.TranslateAsync(
                request.Text,
                detectedLang,
                request.To
            );

            return new TranslateResponseDto
            {
                TranslatedText = translated,
                DetectedSourceLanguage = detectedLang,
                LanguagePair = $"{detectedLang}→{request.To}",
                CharacterCount = request.Text.Length
            };
        }
        // Add to TranslationAppService.cs
        public async Task<Dictionary<string, string>> GetSupportedLanguagesAsync()
            => await _translationService.GetSupportedLanguagesAsync();
    }
}
