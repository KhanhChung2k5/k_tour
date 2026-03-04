using CommunityToolkit.Maui;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.ViewModels;
using HeriStepAI.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace HeriStepAI.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit();
            // Fonts: thêm lại khi có file .ttf trong Resources/Fonts/
            // .ConfigureFonts(fonts =>
            // {
            //     fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            //     fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            // });

        // Services
        builder.Services.AddSingleton<IAuthService, MobileAuthService>();
        builder.Services.AddSingleton<IVoicePreferenceService, VoicePreferenceService>();
        builder.Services.AddSingleton<ILocationSimulatorService, LocationSimulatorService>();
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<IPOIService, POIService>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<ITourSelectionService, TourSelectionService>();
        builder.Services.AddSingleton<ILocalizationService, LocalizationService>();
        builder.Services.AddSingleton<ITourGeneratorService, TourGeneratorService>();

        // ViewModels
        builder.Services.AddSingleton<AuthViewModel>();       // merged login+register
        builder.Services.AddSingleton<LoginPageViewModel>();  // kept for reference
        builder.Services.AddSingleton<RegisterPageViewModel>();
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MapPageViewModel>();
        builder.Services.AddTransient<POIListViewModel>();
        builder.Services.AddTransient<SettingsPageViewModel>();
        builder.Services.AddTransient<TourDetailViewModel>();
        builder.Services.AddTransient<POIDetailViewModel>();

        // Shell & Views
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<AuthPage>();            // merged login+register page
        builder.Services.AddSingleton<LoginPage>();
        builder.Services.AddSingleton<RegisterPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<POIListPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<TourDetailPage>();
        builder.Services.AddTransient<POIDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Configure Android WebView to allow loading external resources (map tiles)
        Microsoft.Maui.Handlers.WebViewHandler.Mapper.AppendToMapping("MapTiles", (handler, view) =>
        {
#if ANDROID
            var settings = handler.PlatformView.Settings;
            settings.AllowUniversalAccessFromFileURLs = true;
            settings.AllowFileAccessFromFileURLs = true;
            settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
            settings.DomStorageEnabled = true;
            settings.JavaScriptEnabled = true;
            settings.SetGeolocationEnabled(true);
            settings.UserAgentString = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36";
#endif
        });

        return builder.Build();
    }
}
