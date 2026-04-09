namespace HeriStepAI.API.Services;

public interface ITranslationService
{
    /// <summary>
    /// Translates text from <paramref name="fromLang"/> to <paramref name="toLang"/>.
    /// Returns null if translation fails.
    /// </summary>
    Task<string?> TranslateAsync(string text, string fromLang, string toLang);

    /// <summary>
    /// Translates <paramref name="text"/> from Vietnamese to all other supported languages.
    /// Returns a dictionary keyed by language code.
    /// </summary>
    Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text);
}
