using HeriStepAI.Mobile.Models;

namespace HeriStepAI.Mobile.Services;

public interface ITourGeneratorService
{
    List<Tour> GenerateSmartTours(List<POI> pois);
}

/// <summary>
/// Smart Tour Generator - Tạo tour tự động dựa trên FoodType và giá
/// </summary>
public class TourGeneratorService : ITourGeneratorService
{
    private readonly ILocalizationService _loc;

    public TourGeneratorService(ILocalizationService localizationService)
    {
        _loc = localizationService;
    }

    public List<Tour> GenerateSmartTours(List<POI> pois)
    {
        var tours = new List<Tour>();

        // Filter only Food POIs
        var foodPOIs = pois.Where(p => p.Category == (int)POICategory.Food).ToList();
        if (foodPOIs.Count == 0) return tours;

        // 1. Tạo tour theo FoodType
        var foodTypeGroups = foodPOIs
            .Where(p => p.FoodType > 0)
            .GroupBy(p => p.FoodType);

        foreach (var group in foodTypeGroups)
        {
            var foodType = (FoodType)group.Key;
            var poisInGroup = group.ToList();

            if (poisInGroup.Count < 2) continue; // Skip if less than 2 POIs

            tours.Add(new Tour
            {
                Id = group.Key,
                Name = GetFoodTypeName(foodType),
                Description = GetFoodTypeDescription(foodType),
                ImageUrl = poisInGroup.FirstOrDefault()?.ImageUrl,
                POIs = poisInGroup,
                POICount = poisInGroup.Count,
                EstimatedMinutes = poisInGroup.Sum(p => p.EstimatedMinutes),
                Rating = poisInGroup.Average(p => p.Rating ?? 4.0),
                ReviewCount = poisInGroup.Sum(p => p.ReviewCount),
                FoodType = group.Key,
                PriceMin = poisInGroup.Min(p => p.PriceMin),
                PriceMax = poisInGroup.Max(p => p.PriceMax),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 2. Tạo tour theo mức giá
        var budgetTours = GenerateBudgetTours(foodPOIs);
        tours.AddRange(budgetTours);

        // 3. Tạo tour "Best Rated" (top rated POIs)
        var topRatedPOIs = foodPOIs
            .Where(p => p.Rating.HasValue && p.Rating >= 4.5)
            .OrderByDescending(p => p.Rating)
            .Take(5)
            .ToList();

        if (topRatedPOIs.Count >= 3)
        {
            tours.Add(new Tour
            {
                Id = 100,
                Name = $"⭐ {_loc.GetString("TourTopRated")}",
                Description = _loc.GetString("TourTopRatedDesc"),
                ImageUrl = topRatedPOIs.FirstOrDefault()?.ImageUrl,
                POIs = topRatedPOIs,
                POICount = topRatedPOIs.Count,
                EstimatedMinutes = topRatedPOIs.Sum(p => p.EstimatedMinutes),
                Rating = topRatedPOIs.Average(p => p.Rating ?? 0),
                ReviewCount = topRatedPOIs.Sum(p => p.ReviewCount),
                FoodType = 0,
                PriceMin = topRatedPOIs.Min(p => p.PriceMin),
                PriceMax = topRatedPOIs.Max(p => p.PriceMax),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // 4. Tạo tour "Quick Bites" (short visit time)
        var quickBitesPOIs = foodPOIs
            .Where(p => p.EstimatedMinutes <= 30)
            .OrderBy(p => p.EstimatedMinutes)
            .Take(5)
            .ToList();

        if (quickBitesPOIs.Count >= 3)
        {
            tours.Add(new Tour
            {
                Id = 101,
                Name = $"⚡ {_loc.GetString("TourQuickEat")}",
                Description = _loc.GetString("TourQuickEatDesc"),
                ImageUrl = quickBitesPOIs.FirstOrDefault()?.ImageUrl,
                POIs = quickBitesPOIs,
                POICount = quickBitesPOIs.Count,
                EstimatedMinutes = quickBitesPOIs.Sum(p => p.EstimatedMinutes),
                Rating = quickBitesPOIs.Average(p => p.Rating ?? 4.0),
                ReviewCount = quickBitesPOIs.Sum(p => p.ReviewCount),
                FoodType = 0,
                PriceMin = quickBitesPOIs.Min(p => p.PriceMin),
                PriceMax = quickBitesPOIs.Max(p => p.PriceMax),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        AppLog.Info($"Generated {tours.Count} smart tours from {foodPOIs.Count} food POIs");
        return tours;
    }

    private List<Tour> GenerateBudgetTours(List<POI> foodPOIs)
    {
        var budgetTours = new List<Tour>();

        // Budget-friendly: < 50,000 VND
        var budgetPOIs = foodPOIs
            .Where(p => p.PriceMax > 0 && p.PriceMax < 50000)
            .OrderBy(p => p.PriceMax)
            .ToList();

        if (budgetPOIs.Count >= 3)
        {
            budgetTours.Add(new Tour
            {
                Id = 200,
                Name = $"💰 {_loc.GetString("TourBudget")}",
                Description = _loc.GetString("TourBudgetDesc"),
                ImageUrl = budgetPOIs.FirstOrDefault()?.ImageUrl,
                POIs = budgetPOIs,
                POICount = budgetPOIs.Count,
                EstimatedMinutes = budgetPOIs.Sum(p => p.EstimatedMinutes),
                Rating = budgetPOIs.Average(p => p.Rating ?? 4.0),
                ReviewCount = budgetPOIs.Sum(p => p.ReviewCount),
                FoodType = 0,
                PriceMin = budgetPOIs.Min(p => p.PriceMin),
                PriceMax = budgetPOIs.Max(p => p.PriceMax),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Mid-range: 50,000 - 150,000 VND
        var midRangePOIs = foodPOIs
            .Where(p => p.PriceMin >= 50000 && p.PriceMax <= 150000)
            .OrderBy(p => p.PriceMin)
            .ToList();

        if (midRangePOIs.Count >= 3)
        {
            budgetTours.Add(new Tour
            {
                Id = 201,
                Name = $"🍽️ {_loc.GetString("TourMidRangeName")}",
                Description = _loc.GetString("TourMidRangeDesc"),
                ImageUrl = midRangePOIs.FirstOrDefault()?.ImageUrl,
                POIs = midRangePOIs,
                POICount = midRangePOIs.Count,
                EstimatedMinutes = midRangePOIs.Sum(p => p.EstimatedMinutes),
                Rating = midRangePOIs.Average(p => p.Rating ?? 4.0),
                ReviewCount = midRangePOIs.Sum(p => p.ReviewCount),
                FoodType = 0,
                PriceMin = midRangePOIs.Min(p => p.PriceMin),
                PriceMax = midRangePOIs.Max(p => p.PriceMax),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Premium: > 150,000 VND
        var premiumPOIs = foodPOIs
            .Where(p => p.PriceMin > 150000)
            .OrderByDescending(p => p.Rating ?? 0)
            .ToList();

        if (premiumPOIs.Count >= 2)
        {
            budgetTours.Add(new Tour
            {
                Id = 202,
                Name = $"👑 {_loc.GetString("TourPremiumName")}",
                Description = _loc.GetString("TourPremiumDesc"),
                ImageUrl = premiumPOIs.FirstOrDefault()?.ImageUrl,
                POIs = premiumPOIs,
                POICount = premiumPOIs.Count,
                EstimatedMinutes = premiumPOIs.Sum(p => p.EstimatedMinutes),
                Rating = premiumPOIs.Average(p => p.Rating ?? 4.0),
                ReviewCount = premiumPOIs.Sum(p => p.ReviewCount),
                FoodType = 0,
                PriceMin = premiumPOIs.Min(p => p.PriceMin),
                PriceMax = premiumPOIs.Max(p => p.PriceMax),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        return budgetTours;
    }

    private string GetFoodTypeName(FoodType foodType) => foodType switch
    {
        FoodType.Seafood => $"🦞 {_loc.GetString("TourGen_SeafoodName")}",
        FoodType.Vegetarian => $"🥗 {_loc.GetString("TourGen_VegetarianName")}",
        FoodType.Vietnamese => $"🇻🇳 {_loc.GetString("TourGen_VietnameseName")}",
        FoodType.StreetFood => $"🍜 {_loc.GetString("TourGen_StreetName")}",
        FoodType.BBQ => $"🔥 {_loc.GetString("TourGen_BBQName")}",
        FoodType.Noodles => $"🍝 {_loc.GetString("TourGen_NoodlesName")}",
        _ => $"🍴 {_loc.GetString("TourGen_DefaultName")}"
    };

    private string GetFoodTypeDescription(FoodType foodType) => foodType switch
    {
        FoodType.Seafood => _loc.GetString("TourGen_SeafoodDesc"),
        FoodType.Vegetarian => _loc.GetString("TourGen_VegetarianDesc"),
        FoodType.Vietnamese => _loc.GetString("TourGen_VietnameseDesc"),
        FoodType.StreetFood => _loc.GetString("TourGen_StreetDesc"),
        FoodType.BBQ => _loc.GetString("TourGen_BBQDesc"),
        FoodType.Noodles => _loc.GetString("TourGen_NoodlesDesc"),
        _ => _loc.GetString("TourGen_DefaultDesc")
    };
}
