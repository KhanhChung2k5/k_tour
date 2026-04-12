using System.ComponentModel.DataAnnotations;

namespace HeriStepAI.Web.Models;

/// <summary>Form tạo POI mới (chủ quán tự thêm) — không gồm tài khoản.</summary>
public class ShopOwnerPOICreateViewModel
{
    [Required(ErrorMessage = "Tên POI là bắt buộc")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả là bắt buộc")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vĩ độ là bắt buộc")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Kinh độ là bắt buộc")]
    public double Longitude { get; set; }

    public string? Address { get; set; }
    public double Radius { get; set; } = 50;
    public int Priority { get; set; } = 1;
    public string? ImageUrl { get; set; }
    public string? MapLink { get; set; }
    public bool IsActive { get; set; } = true;

    [Required(ErrorMessage = "Loại POI là bắt buộc")]
    public int Category { get; set; } = 2;

    public int? TourId { get; set; }
    public int EstimatedMinutes { get; set; } = 30;
    public int FoodType { get; set; }
    public long PriceMin { get; set; }
    public long PriceMax { get; set; }

    public string? TextContent_vi { get; set; }
    public string? TextContent_en { get; set; }
    public string? TextContent_ko { get; set; }
    public string? TextContent_zh { get; set; }
    public string? TextContent_ja { get; set; }
    public string? TextContent_th { get; set; }
    public string? TextContent_fr { get; set; }
}
