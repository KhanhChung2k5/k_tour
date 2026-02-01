using SQLite;

namespace HeriStepAI.Mobile.Models;

public class Tour
{
    [PrimaryKey]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int EstimatedMinutes { get; set; }
    public int POICount { get; set; }
    public double? Rating { get; set; }
    public int ReviewCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Ignore]
    public List<POI> POIs { get; set; } = new();
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
