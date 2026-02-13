using DotNetEnv;
using HeriStepAI.API.Data;
using HeriStepAI.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

// Load .env
var envPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), ".env")
};
foreach (var p in envPaths) { if (File.Exists(p)) { Env.Load(p); break; } }

var builder = WebApplication.CreateBuilder(args);

var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var portNum) ? portNum : 5001;
builder.WebHost.ConfigureKestrel(options => options.ListenAnyIP(port));

// Chỉ load controllers từ Web assembly, không load từ API
builder.Services.AddControllersWithViews()
    .ConfigureApplicationPartManager(manager =>
    {
        var apiPart = manager.ApplicationParts
            .FirstOrDefault(p => p.Name == "HeriStepAI.API");
        if (apiPart != null)
        {
            manager.ApplicationParts.Remove(apiPart);
        }
    });

// Database - dùng env SUPABASE_CONNECTION_STRING khi deploy lên Render
var connectionString = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Fix and convert connection string
if (!string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = connectionString.Trim();

    // If URI format (postgresql://...), convert to Npgsql key-value format
    if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var uri = new Uri(connectionString.Replace("?sslmode=", "?sslmode=require").Replace("?sslmode", "?sslmode=require"));
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var database = uri.AbsolutePath.TrimStart('/');

            connectionString = $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};SSL Mode=Require;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=10;Connection Lifetime=300";
            Console.WriteLine($"[Web] Converted URI connection string to Npgsql format");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Web] Error converting connection string: {ex.Message}");
        }
    }
    // Fix key-value format if needed
    else if (connectionString.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
    {
        if (!connectionString.Contains("Pooling", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ";Pooling=true;Minimum Pool Size=0;Maximum Pool Size=10;Connection Lifetime=300";
        }
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Cookie authentication - đơn giản, ổn định hơn JWT Bearer cho Web MVC
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

// Supabase Storage
builder.Services.AddSingleton<ISupabaseStorageService, SupabaseStorageService>();

// HTTP Client for API
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("API_BASE_URL") ?? builder.Configuration["ApiSettings:BaseUrl"] ?? "https://heristep.onrender.com/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Bỏ HTTPS redirect khi dùng HTTP local
// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
