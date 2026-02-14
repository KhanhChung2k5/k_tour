using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HeriStepAI.Mobile;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localizationService;

    public AppShell(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        _localizationService = serviceProvider.GetRequiredService<ILocalizationService>();

        // Set initial tab titles
        UpdateTabTitles();

        // Subscribe to language changes
        _localizationService.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(UpdateTabTitles);

        // Tạo trang qua DI - bắt lỗi từng trang để tránh crash
        MainTab.Content = CreatePageSafe(serviceProvider, "MainPage", () => serviceProvider.GetRequiredService<MainPage>());
        MapTab.Content = CreatePageSafe(serviceProvider, "MapPage", () => serviceProvider.GetRequiredService<MapPage>());
        POIListTab.Content = CreatePageSafe(serviceProvider, "POIListPage", () => serviceProvider.GetRequiredService<POIListPage>());
        SettingsTab.Content = CreatePageSafe(serviceProvider, "SettingsPage", () => serviceProvider.GetRequiredService<SettingsPage>());

        // Register navigation routes
        Routing.RegisterRoute("TourDetailPage", typeof(TourDetailPage));
        Routing.RegisterRoute("POIDetailPage", typeof(POIDetailPage));
    }

    private void UpdateTabTitles()
    {
        MainTab.Title = _localizationService.GetString("Home");
        MapTab.Title = _localizationService.GetString("Map");
        POIListTab.Title = _localizationService.GetString("Places");
        SettingsTab.Title = _localizationService.GetString("Settings");
    }

    static ContentPage CreatePageSafe(IServiceProvider sp, string name, Func<ContentPage> create)
    {
        try
        {
            return create();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRASH] Failed to create {name}: {ex}");
            return new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    Padding = 20,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label { Text = $"Lỗi tải {name}", FontSize = 18, FontAttributes = FontAttributes.Bold },
                        new Label { Text = ex.Message, FontSize = 12, TextColor = Colors.Red }
                    }
                }
            };
        }
    }
}
