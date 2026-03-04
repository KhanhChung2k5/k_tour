using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class AuthPage : ContentPage
{
    public AuthPage(AuthViewModel viewModel)
    {
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        System.Diagnostics.Debug.WriteLine("[AuthPage] InitializeComponent START");
        InitializeComponent();
        var ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000 / System.Diagnostics.Stopwatch.Frequency;
        System.Diagnostics.Debug.WriteLine($"[AuthPage] InitializeComponent DONE in {ms}ms");
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Load hero image after first paint to avoid blocking main thread at startup (ANR)
        if (HeroImage != null && HeroImage.Source == null)
        {
            _ = LoadHeroImageAsync();
        }
    }

    private async Task LoadHeroImageAsync()
    {
        await Task.Delay(50);
        var src = ImageSource.FromFile("icon_scene_final_512.png");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (HeroImage != null)
                HeroImage.Source = src;
        });
    }
}
