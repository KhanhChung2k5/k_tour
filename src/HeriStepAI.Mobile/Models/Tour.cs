using SQLite;
using HeriStepAI.Mobile.Services;

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

    /// <summary>Loại món ăn: 0=Khác, 1=Hải sản, 2=Món chay, 3=Đặc sản, ...</summary>
    public int FoodType { get; set; }
    /// <summary>Giá tối thiểu (VND)</summary>
    public long PriceMin { get; set; }
    /// <summary>Giá tối đa (VND)</summary>
    public long PriceMax { get; set; }

    [Ignore]
    public List<POI> POIs { get; set; } = new();

    [Ignore]
    public string PriceRangeText => PriceMin > 0 || PriceMax > 0
        ? $"{PriceMin:N0} - {PriceMax:N0}đ"
        : "";

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
    public string POICountText
    {
        get
        {
            var loc = GetLocalizationService();
            return $"{POICount} {loc.GetString("Points")}";
        }
    }

    [Ignore]
    public string ShopCountText
    {
        get
        {
            var loc = GetLocalizationService();
            return $"{POICount} {loc.GetString("Shops")}";
        }
    }

    private static ILocalizationService GetLocalizationService()
    {
        try { return IPlatformApplication.Current!.Services.GetRequiredService<ILocalizationService>(); }
        catch { return new LocalizationService(); }
    }
}

/// <summary>Loại món ăn cho tour ẩm thực</summary>
public enum FoodType
{
    Other = 0,
    Seafood = 1,      // Hải sản
    Vegetarian = 2,   // Món chay
    Vietnamese = 3,   // Đặc sản Việt Nam
    StreetFood = 4,   // Ẩm thực đường phố
    BBQ = 5,          // Nướng
    Noodles = 6       // Bún, phở, mì
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
