using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class SeedService
{
    private readonly ApplicationDbContext _context;

    public SeedService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync())
            return;

        // Create admin user
        var admin = new User
        {
            Username = "admin",
            Email = "admin@heristepai.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Create shop owner
        var shopOwner = new User
        {
            Username = "shopowner1",
            Email = "owner@shop.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("owner123"),
            Role = UserRole.ShopOwner,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.Users.AddRange(admin, shopOwner);
        await _context.SaveChangesAsync();

        // Create sample POIs
        var poi1 = new POI
        {
            Name = "Nhà hàng Hải Sản Đà Nẵng",
            Description = "Nhà hàng hải sản tươi ngon tại Đà Nẵng",
            Latitude = 16.0544,
            Longitude = 108.2022,
            Radius = 50,
            Priority = 1,
            OwnerId = shopOwner.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var poi2 = new POI
        {
            Name = "Quán Cà Phê Bờ Biển",
            Description = "Quán cà phê view biển đẹp",
            Latitude = 16.0600,
            Longitude = 108.2100,
            Radius = 30,
            Priority = 2,
            OwnerId = shopOwner.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.POIs.AddRange(poi1, poi2);
        await _context.SaveChangesAsync();

        // Add content for POIs
        var content1 = new POIContent
        {
            POId = poi1.Id,
            Language = "vi",
            TextContent = "Chào mừng bạn đến với Nhà hàng Hải Sản Đà Nẵng. Chúng tôi chuyên phục vụ các món hải sản tươi ngon được đánh bắt hàng ngày.",
            ContentType = ContentType.TTS,
            CreatedAt = DateTime.UtcNow
        };

        var content2 = new POIContent
        {
            POId = poi1.Id,
            Language = "en",
            TextContent = "Welcome to Da Nang Seafood Restaurant. We specialize in fresh seafood caught daily.",
            ContentType = ContentType.TTS,
            CreatedAt = DateTime.UtcNow
        };

        _context.POIContents.AddRange(content1, content2);
        await _context.SaveChangesAsync();
    }
}
