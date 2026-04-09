using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace HeriStepAI.API.Services;

/// <summary>
/// Translates text using the MyMemory free API (https://mymemory.translated.net/).
/// No API key required. Free quota: ~5000 chars/day per IP.
/// </summary>
public class MyMemoryTranslationService : ITranslationService
{
    private readonly HttpClient _http;

    // App uses these language codes; MyMemory uses slightly different ones for some.
    private static readonly Dictionary<string, string> _myMemoryCodeMap = new()
    {
        ["vi"] = "vi",
        ["en"] = "en",
        ["ko"] = "ko",
        ["zh"] = "zh-CN",   // MyMemory uses zh-CN for Simplified Chinese
        ["ja"] = "ja",
        ["th"] = "th",
        ["fr"] = "fr",
    };

    // All target languages to translate into (excluding source "vi")
    private static readonly string[] _targetLanguages = { "en", "ko", "zh", "ja", "th", "fr" };

    public MyMemoryTranslationService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient("MyMemory");
    }

    public async Task<string?> TranslateAsync(string text, string fromLang, string toLang)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        try
        {
            var from = _myMemoryCodeMap.GetValueOrDefault(fromLang, fromLang);
            var to   = _myMemoryCodeMap.GetValueOrDefault(toLang, toLang);

            var encoded = Uri.EscapeDataString(text);
            var url = $"https://api.mymemory.translated.net/get?q={encoded}&langpair={from}|{to}&de=khanhcong460@gmail.com";

            var response = await _http.GetFromJsonAsync<MyMemoryResponse>(url);

            if (response?.ResponseStatus == 200 &&
                !string.IsNullOrWhiteSpace(response.ResponseData?.TranslatedText))
            {
                return response.ResponseData.TranslatedText;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Translation] Error translating to {toLang}: {ex.Message}");
        }

        return null;
    }

    public async Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text)
    {
        var results = new Dictionary<string, string>();

        // Translate to all target languages in parallel (max 3 concurrent to be respectful)
        var semaphore = new SemaphoreSlim(3, 3);
        var tasks = _targetLanguages.Select(async lang =>
        {
            await semaphore.WaitAsync();
            try
            {
                var translated = await TranslateAsync(text, "vi", lang);
                if (!string.IsNullOrWhiteSpace(translated))
                    lock (results) { results[lang] = translated; }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }

    // ── MyMemory response models ──────────────────────────────────────────
    private class MyMemoryResponse
    {
        [JsonPropertyName("responseData")]
        public MyMemoryData? ResponseData { get; set; }

        [JsonPropertyName("responseStatus")]
        public int ResponseStatus { get; set; }
    }

    private class MyMemoryData
    {
        [JsonPropertyName("translatedText")]
        public string? TranslatedText { get; set; }
    }
}
