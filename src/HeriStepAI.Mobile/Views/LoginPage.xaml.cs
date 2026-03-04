using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginPageViewModel viewModel)
    {
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        System.Diagnostics.Debug.WriteLine("[LoginPage] InitializeComponent START");
        InitializeComponent();
        var ms = (System.Diagnostics.Stopwatch.GetTimestamp() - t0) * 1000 / System.Diagnostics.Stopwatch.Frequency;
        System.Diagnostics.Debug.WriteLine($"[LoginPage] InitializeComponent DONE in {ms}ms");
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (HeroImage != null && HeroImage.Source == null)
            _ = LoadHeroImageAsync();
    }

    private async Task LoadHeroImageAsync()
    {
        await Task.Delay(50);
        var src = ImageSource.FromFile("icon_scene_final_512.png");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (HeroImage != null) HeroImage.Source = src;
        });
    }
}
