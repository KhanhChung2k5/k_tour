using System.Text.Json.Serialization;

namespace HeriStepAI.API.Models;

public class POI
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public double Radius { get; set; } = 50; // meters
    public int Priority { get; set; } = 1;
    public int? OwnerId { get; set; }
    [JsonIgnore]
    public User? Owner { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // New fields for enhanced UI
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public int Category { get; set; } // POICategory enum value
    public int? TourId { get; set; }
    public int EstimatedMinutes { get; set; } = 30; // Estimated visit time
    
    public List<POIContent> Contents { get; set; } = new();
}

public enum POICategory
{
    All = 0,
    Sightseeing = 1,    // Tham quan
    Food = 2,           // Ẩm thực
    Accommodation = 3,  // Nghỉ dưỡng
    Shopping = 4,       // Mua sắm
    Entertainment = 5,  // Giải trí
    Historical = 6,     // Di tích lịch sử
    Nature = 7          // Thiên nhiên
}
