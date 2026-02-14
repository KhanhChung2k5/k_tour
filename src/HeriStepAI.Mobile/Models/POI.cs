using SQLite;
using HeriStepAI.Mobile.Services;

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
    
    // Food-specific: loại món ăn (FoodType enum), giá VND
    public int FoodType { get; set; }
    public long PriceMin { get; set; }
    public long PriceMax { get; set; }
    
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
    public string CategoryText
    {
        get
        {
            var loc = GetLocalizationService();
            return (POICategory)Category switch
            {
                POICategory.Sightseeing => loc.GetString("CatSightseeing"),
                POICategory.Food => loc.GetString("CatFood"),
                POICategory.Accommodation => loc.GetString("CatAccommodation"),
                POICategory.Shopping => loc.GetString("CatShopping"),
                POICategory.Entertainment => loc.GetString("CatEntertainment"),
                POICategory.Historical => loc.GetString("CatHistorical"),
                POICategory.Nature => loc.GetString("CatNature"),
                _ => loc.GetString("CatAll")
            };
        }
    }

    [Ignore]
    public string FoodTypeText
    {
        get
        {
            var loc = GetLocalizationService();
            return this.FoodType switch
            {
                1 => loc.GetString("FoodSeafood"),
                2 => loc.GetString("FoodVegetarian"),
                3 => loc.GetString("FoodSpecialty"),
                4 => loc.GetString("FoodStreet"),
                5 => loc.GetString("FoodGrilled"),
                6 => loc.GetString("FoodNoodles"),
                _ => ""
            };
        }
    }

    private static ILocalizationService GetLocalizationService()
    {
        try { return IPlatformApplication.Current!.Services.GetRequiredService<ILocalizationService>(); }
        catch { return new Services.LocalizationService(); }
    }
    
    [Ignore]
    public string EstimatedTimeText
    {
        get
        {
            var loc = GetLocalizationService();
            return $"{EstimatedMinutes} {loc.GetString("Minutes")}";
        }
    }

    [Ignore]
    public string PriceRangeText => PriceMin > 0 || PriceMax > 0
        ? $"{PriceMin:N0} - {PriceMax:N0}đ"
        : "";

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
