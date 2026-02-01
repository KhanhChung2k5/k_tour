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
        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IGeofenceService, GeofenceService>();
        builder.Services.AddSingleton<INarrationService, NarrationService>();
        builder.Services.AddSingleton<IPOIService, POIService>();
        builder.Services.AddSingleton<IApiService, ApiService>();

        // ViewModels
        builder.Services.AddTransient<MainPageViewModel>();
        builder.Services.AddTransient<MapPageViewModel>();
        builder.Services.AddTransient<POIListViewModel>();
        builder.Services.AddTransient<SettingsPageViewModel>();

        // Shell & Views
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddTransient<POIListPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
