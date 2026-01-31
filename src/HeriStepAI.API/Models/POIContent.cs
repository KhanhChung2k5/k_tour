namespace HeriStepAI.API.Models;

public class POIContent
{
    public int Id { get; set; }
    public int POId { get; set; }
    public POI POI { get; set; } = null!;
    public string Language { get; set; } = "vi"; // vi, en, zh, etc.
    public string? TextContent { get; set; } // For TTS
    public string? AudioUrl { get; set; } // Pre-recorded audio
    public ContentType ContentType { get; set; } = ContentType.TTS;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ContentType
{
    TTS = 1,
    AudioFile = 2
}
