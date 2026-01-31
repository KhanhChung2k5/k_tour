namespace HeriStepAI.Mobile.Models;

public class POI
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Radius { get; set; }
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public List<POIContent> Contents { get; set; } = new();
}

public class POIContent
{
    public int Id { get; set; }
    public int POId { get; set; }
    public string Language { get; set; } = "vi";
    public string? TextContent { get; set; }
    public string? AudioUrl { get; set; }
    public ContentType ContentType { get; set; }
}

public enum ContentType
{
    TTS = 1,
    AudioFile = 2
}
