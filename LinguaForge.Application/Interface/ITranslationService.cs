

namespace LinguaForge.Application.Interface
{
    public interface ITranslationService
    {
        /// <summary>
        /// Translates text from one language to another.
        /// Pass empty string for 'from' to trigger auto-detection.
        /// </summary>
        Task<string> TranslateAsync(string text, string from, string to);

        /// <summary>
        /// Detects the language of the given text.
        /// Returns BCP-47 language code e.g. "en", "de", "hi"
        /// </summary>
        Task<string> DetectLanguageAsync(string text);

        /// <summary>
        /// Translates into multiple target languages in a single API call.
        /// More efficient than calling TranslateAsync repeatedly.
        /// </summary>
        Task<Dictionary<string, string>> TranslateToMultipleAsync(
            string text,
            string from,
            IEnumerable<string> targetLanguages);

        /// <summary>
        /// Returns all language codes + display names supported by Azure Translator.
        /// Useful for populating language dropdowns in the UI.
        /// </summary>
        Task<Dictionary<string, string>> GetSupportedLanguagesAsync();
    }
}
