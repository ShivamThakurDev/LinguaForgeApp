using LinguaForge.Application.Interface;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace LinguaForge.Infrastructure.Services
{
    public class AzureTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _region;
        private const string BaseUrl = "https://api.cognitive.microsofttranslator.com";

        public AzureTranslationService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["AzureTranslator:ApiKey"] ?? string.Empty;
            _region = config["AzureTranslator:Region"] ?? string.Empty;
        }

        public async Task<string> TranslateAsync(string text, string from, string to)
        {
            if (!CanCallAzure())
            {
                return $"[mock-{to}] {text}";
            }

            var fromParam = string.IsNullOrWhiteSpace(from) ? "" : $"&from={from}";
            var url = $"{BaseUrl}/translate?api-version=3.0{fromParam}&to={to}";

            var body = JsonSerializer.Serialize(new[] { new { Text = text } });
            using var request = BuildRequest(HttpMethod.Post, url, body);
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement[0]
                      .GetProperty("translations")[0]
                      .GetProperty("text")
                      .GetString() ?? string.Empty;
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            if (!CanCallAzure())
            {
                return "en";
            }

            var url = $"{BaseUrl}/detect?api-version=3.0";
            var body = JsonSerializer.Serialize(new[] { new { Text = text } });

            using var request = BuildRequest(HttpMethod.Post, url, body);
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement[0]
                      .GetProperty("language")
                      .GetString() ?? "unknown";
        }

        public async Task<Dictionary<string, string>> TranslateToMultipleAsync(
            string text,
            string from,
            IEnumerable<string> targetLanguages)
        {
            var targets = targetLanguages.ToList();
            if (targets.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            if (!CanCallAzure())
            {
                return targets.ToDictionary(x => x, x => $"[mock-{x}] {text}");
            }

            var toParams = string.Join("", targets.Select(t => $"&to={t}"));
            var fromParam = string.IsNullOrWhiteSpace(from) ? "" : $"&from={from}";
            var url = $"{BaseUrl}/translate?api-version=3.0{fromParam}{toParams}";

            var body = JsonSerializer.Serialize(new[] { new { Text = text } });
            using var request = BuildRequest(HttpMethod.Post, url, body);
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var results = new Dictionary<string, string>();
            var translations = doc.RootElement[0].GetProperty("translations");

            foreach (var translation in translations.EnumerateArray())
            {
                var lang = translation.GetProperty("to").GetString() ?? "";
                var translated = translation.GetProperty("text").GetString() ?? "";
                results[lang] = translated;
            }

            return results;
        }

        public async Task<Dictionary<string, string>> GetSupportedLanguagesAsync()
        {
            var url = $"{BaseUrl}/languages?api-version=3.0&scope=translation";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var result = new Dictionary<string, string>();
            var translationScope = doc.RootElement.GetProperty("translation");

            foreach (var lang in translationScope.EnumerateObject())
            {
                var displayName = lang.Value.GetProperty("name").GetString() ?? lang.Name;
                result[lang.Name] = displayName;
            }

            return result;
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string body)
        {
            var request = new HttpRequestMessage(method, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
            return request;
        }

        private bool CanCallAzure() =>
            !string.IsNullOrWhiteSpace(_apiKey) &&
            !string.IsNullOrWhiteSpace(_region);
    }
}