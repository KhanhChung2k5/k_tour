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
if (!string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("pooler.supabase.com", StringComparison.OrdinalIgnoreCase))
{
    connectionString = connectionString.Trim();
    if (connectionString.EndsWith("?sslmode", StringComparison.OrdinalIgnoreCase))
        connectionString += "=Require";
    else if (connectionString.EndsWith("&sslmode", StringComparison.OrdinalIgnoreCase))
        connectionString += "=Require";
    else if (!connectionString.Contains("sslmode=", StringComparison.OrdinalIgnoreCase))
        connectionString += (connectionString.Contains("?") ? "&" : "?") + "sslmode=Require";
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
