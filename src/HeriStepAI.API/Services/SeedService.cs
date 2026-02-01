using HeriStepAI.API.Data;
using HeriStepAI.API.Models;
using Microsoft.EntityFrameworkCore;

namespace HeriStepAI.API.Services;

public class SeedService
{
    private readonly ApplicationDbContext _context;

    private const string AdminEmail = "admin@heristepai.com";
    private const string AdminPassword = "admin123";

    public SeedService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Đảm bảo có admin. force=true: reset mật khẩu admin về admin123.
    /// </summary>
    public async Task<(bool Created, string Message)> EnsureAdminAsync(bool force = false)
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == AdminEmail);
        if (admin != null)
        {
            if (force)
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword);
                admin.IsActive = true;
                await _context.SaveChangesAsync();
                return (false, "Admin password reset. Login: admin@heristepai.com / admin123");
            }
            return (false, "Admin exists. Login: admin@heristepai.com / admin123");
        }

        if (!await _context.Users.AnyAsync())
        {
            await SeedAsync();
            return (true, "Seed completed. Login: admin@heristepai.com / admin123");
        }

        var newAdmin = new User
        {
            Username = "admin",
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        _context.Users.Add(newAdmin);
        await _context.SaveChangesAsync();
        return (true, "Admin created. Login: admin@heristepai.com / admin123");
    }

    public async Task SeedAsync()
    {
        if (await _context.Users.AnyAsync())
            return;

        var admin = new User
        {
            Username = "admin",
            Email = AdminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword),
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
