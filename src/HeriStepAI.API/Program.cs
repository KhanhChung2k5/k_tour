using HeriStepAI.API.Data;
using HeriStepAI.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel - dùng PORT từ Render (mặc định 5000)
var port = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var p) ? p : 5000;
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
});

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original casing
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Database - Supabase PostgreSQL (Secret File tránh Render cắt env tại dấu =)
var connectionString = File.Exists("/etc/secrets/SUPABASE_CONNECTION_STRING")
    ? File.ReadAllText("/etc/secrets/SUPABASE_CONNECTION_STRING").Trim()
    : Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING") 
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("SUPABASE_CONNECTION_STRING is required. Set it in Render Environment Variables.");

// Convert PostgreSQL URI format to ADO.NET key-value format for Npgsql
if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
    || connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var database = uri.AbsolutePath.TrimStart('/');

    var sb = new StringBuilder();
    sb.Append($"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password}");

    // Parse query string params (e.g. ?sslmode=Require)
    var hasSslMode = false;
    var queryString = uri.Query.TrimStart('?');
    if (!string.IsNullOrEmpty(queryString))
    {
        foreach (var param in queryString.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = param.Split('=', 2);
            var key = parts[0];
            var value = parts.Length > 1 ? parts[1] : "";
            if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                hasSslMode = true;
                if (!string.IsNullOrEmpty(value))
                    sb.Append($";SSL Mode={value}");
            }
            else if (!string.IsNullOrEmpty(key))
            {
                sb.Append($";{key}={value}");
            }
        }
    }

    if (!hasSslMode || string.IsNullOrEmpty(queryString))
        sb.Append(";SSL Mode=Require");
    sb.Append(";Trust Server Certificate=true");

    connectionString = sb.ToString();
}
else if (!connectionString.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase)
    && !connectionString.Contains("sslmode", StringComparison.OrdinalIgnoreCase))
{
    connectionString += ";SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// JWT Authentication - Read from .env or appsettings.json
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? jwtSettings["SecretKey"] 
    ?? "YourSuperSecretKeyForJWTTokenGeneration12345";
    
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
    ?? jwtSettings["Issuer"] 
    ?? "HeriStepAI";
    
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
    ?? jwtSettings["Audience"] 
    ?? "HeriStepAIUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPOIService, POIService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IGeocodingService, GeocodingService>();

var app = builder.Build();

// Swagger - bật cả Production để test API
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HeriStepAI API v1"));

// app.UseHttpsRedirection(); // Disabled for development
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Redirect root to Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// Initialize database (wrap in try/catch so app still starts if DB is unreachable)
try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            // Log but continue startup
            Console.WriteLine($"Warning: Failed to ensure database created: {ex.Message}");
        }

        try
        {
            // Seed initial data
            var seedService = new SeedService(db);
            await seedService.SeedAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to seed database: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    // If creating scope or resolving services fails, log and continue
    Console.WriteLine($"Warning: Database initialization skipped: {ex.Message}");
}

app.Run();
