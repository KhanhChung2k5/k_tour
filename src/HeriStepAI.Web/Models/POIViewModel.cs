namespace HeriStepAI.Web.Models;

public class POIViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Address { get; set; }
    public double Radius { get; set; } = 50;
    public int Priority { get; set; } = 1;
    public int? OwnerId { get; set; }
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public double? Rating { get; set; }
    public int ReviewCount { get; set; } = 0;
    public int Category { get; set; }
    public int? TourId { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public int FoodType { get; set; }
    public long PriceMin { get; set; }
    public long PriceMax { get; set; }

    // Content fields (for form input)
    public string? TextContent_vi { get; set; }
    public string? TextContent_en { get; set; }
    public string? TextContent_ko { get; set; }
    public string? TextContent_zh { get; set; }
    public string? TextContent_ja { get; set; }
    public string? TextContent_th { get; set; }
    public string? TextContent_fr { get; set; }

    // Contents list (for API)
    public List<POIContentViewModel>? Contents { get; set; }

    // Helper properties for display
    public string CategoryName => ((POICategory)Category).ToString();
    public string FoodTypeName => ((FoodType)FoodType).ToString();
    public string PriceRange => $"{PriceMin:N0} - {PriceMax:N0} VND";
}

public class POIContentViewModel
{
    public int Id { get; set; }
    public int POId { get; set; }
    public string Language { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? AudioUrl { get; set; }
    public int ContentType { get; set; } = 1; // 1 = TTS
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum POICategory
{
    All = 0,
    Sightseeing = 1,
    Food = 2,
    Accommodation = 3,
    Shopping = 4,
    Entertainment = 5,
    Historical = 6,
    Nature = 7
}

public enum FoodType
{
    Other = 0,
    Seafood = 1,
    Vegetarian = 2,
    Specialty = 3,
    Street = 4,
    Grilled = 5,
    Noodles = 6
}
