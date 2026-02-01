using SQLite;

namespace HeriStepAI.Mobile.Models;

public class POI
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public double Radius { get; set; }
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    
    // New fields for UI
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public int Category { get; set; } // POICategory enum value
    public int? TourId { get; set; }
    public int EstimatedMinutes { get; set; } = 30; // Estimated visit time
    
    // Calculated property for distance (set at runtime)
    [Ignore]
    public double? DistanceMeters { get; set; }
    
    [Ignore]
    public string DistanceText => DistanceMeters.HasValue 
        ? DistanceMeters.Value < 1000 
            ? $"{DistanceMeters.Value:F0}m" 
            : $"{DistanceMeters.Value / 1000:F1}km"
        : "";
    
    [Ignore]
    public string RatingText => Rating.HasValue ? $"{Rating.Value:F1}" : "N/A";
    
    [Ignore]
    public string CategoryText => (POICategory)Category switch
    {
        POICategory.Sightseeing => "Tham quan",
        POICategory.Food => "Ẩm thực",
        POICategory.Accommodation => "Nghỉ dưỡng",
        POICategory.Shopping => "Mua sắm",
        POICategory.Entertainment => "Giải trí",
        POICategory.Historical => "Di tích",
        POICategory.Nature => "Thiên nhiên",
        _ => "Tất cả"
    };
    
    [Ignore]
    public List<POIContent> Contents { get; set; } = new();
}

public class POIContent
{
    [PrimaryKey]
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
