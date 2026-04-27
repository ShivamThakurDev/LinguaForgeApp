using LinguaForge.Application.Interface;


namespace LinguaForge.Infrastructure.Services
{
    public class MockTranslationService : ITranslationService
    {
        public Task<string> TranslateAsync(string text, string from, string to)
            => Task.FromResult($"[{to.ToUpper()}] {text}");

        public Task<string> DetectLanguageAsync(string text)
            => Task.FromResult("en");

        public Task<Dictionary<string, string>> TranslateToMultipleAsync(
            string text, string from, IEnumerable<string> targetLanguages)
        {
            var result = targetLanguages.ToDictionary(
                lang => lang,
                lang => $"[{lang.ToUpper()}] {text}"
            );
            return Task.FromResult(result);
        }

        public Task<Dictionary<string, string>> GetSupportedLanguagesAsync()
        {
            return Task.FromResult(new Dictionary<string, string>
            {
                { "en", "English" },
                { "de", "German" },
                { "hi", "Hindi" },
                { "fr", "French" },
                { "es", "Spanish" }
            });
        }
    }
}

