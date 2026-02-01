using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage() : this(GetViewModel()) { }

    public SettingsPage(SettingsPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    static SettingsPageViewModel GetViewModel() =>
        App.Services?.GetService<SettingsPageViewModel>()
        ?? throw new InvalidOperationException("SettingsPageViewModel not found. Check DI.");
}
