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

        // Chỉ tạo tab đầu tiên để tránh ANR; các tab còn lại tạo khi user chọn lần đầu
        MainTab.Content = CreatePageSafe(_serviceProvider, "MainPage", () => _serviceProvider.GetRequiredService<MainPage>());

        Navigated += OnShellNavigated;

        Routing.RegisterRoute("TourDetailPage", typeof(TourDetailPage));
        Routing.RegisterRoute("POIDetailPage", typeof(POIDetailPage));
    }

    private void OnShellNavigated(object? sender, ShellNavigatedEventArgs e)
    {
        EnsureCurrentTabContent();
    }

    private void EnsureCurrentTabContent()
    {
        var content = CurrentItem?.CurrentItem?.CurrentItem as ShellContent;
        if (content == null) return;
        if (content.Content != null) return;

        if (content == MapTab)
            content.Content = CreatePageSafe(_serviceProvider, "MapPage", () => _serviceProvider.GetRequiredService<MapPage>());
        else if (content == POIListTab)
            content.Content = CreatePageSafe(_serviceProvider, "POIListPage", () => _serviceProvider.GetRequiredService<POIListPage>());
        else if (content == SettingsTab)
            content.Content = CreatePageSafe(_serviceProvider, "SettingsPage", () => _serviceProvider.GetRequiredService<SettingsPage>());
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
