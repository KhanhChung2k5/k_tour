using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;
using Microsoft.Extensions.DependencyInjection;

namespace HeriStepAI.Mobile;

public partial class AppShell : Shell
{
    private readonly ILocalizationService _localizationService;
    private readonly IServiceProvider _serviceProvider;

    public AppShell(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _localizationService = serviceProvider.GetRequiredService<ILocalizationService>();

        UpdateTabTitles();
        _localizationService.LanguageChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(UpdateTabTitles);

        // Use ContentTemplate (NOT Content) for ALL tabs.
        // MAUI calls the factory lazily when each tab is first rendered, which:
        //   1. Fixes the CRASH: "No Content found for ShellContent" — the old Navigated-based
        //      lazy approach fired AFTER Android Fragment.OnCreateView already needed the content.
        //      ContentTemplate is read by GetOrCreateContent() synchronously, so it is always ready.
        //   2. Reduces startup blocking: no heavy InitializeComponent() in AppShell constructor.
        MainTab.ContentTemplate     = new DataTemplate(() => CreatePageSafe(_serviceProvider, "MainPage",    () => _serviceProvider.GetRequiredService<MainPage>()));
        MapTab.ContentTemplate      = new DataTemplate(() => CreatePageSafe(_serviceProvider, "MapPage",     () => _serviceProvider.GetRequiredService<MapPage>()));
        POIListTab.ContentTemplate  = new DataTemplate(() => CreatePageSafe(_serviceProvider, "POIListPage", () => _serviceProvider.GetRequiredService<POIListPage>()));
        SettingsTab.ContentTemplate = new DataTemplate(() => CreatePageSafe(_serviceProvider, "SettingsPage",() => _serviceProvider.GetRequiredService<SettingsPage>()));

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
