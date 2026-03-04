using HeriStepAI.Mobile.ViewModels;
using HeriStepAI.Mobile.Helpers;

namespace HeriStepAI.Mobile.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage() : this(GetViewModel()) { }

    public SettingsPage(SettingsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // Apply responsive padding
        SettingsHeader.Padding = ResponsiveHelper.HeaderPadding();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is SettingsPageViewModel vm)
            vm.RefreshAccountInfo();
    }

    static SettingsPageViewModel GetViewModel() =>
        App.Services?.GetService<SettingsPageViewModel>()
        ?? throw new InvalidOperationException("SettingsPageViewModel not found. Check DI.");
}
