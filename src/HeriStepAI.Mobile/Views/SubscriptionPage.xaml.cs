using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class SubscriptionPage : ContentPage
{
    private readonly SubscriptionViewModel _viewModel;

    public SubscriptionPage(SubscriptionViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnCopyRefTapped(object sender, TappedEventArgs e)
    {
        var text = _viewModel.TransferRef;
        if (string.IsNullOrEmpty(text)) return;

        await Clipboard.Default.SetTextAsync(text);
        await DisplayAlert("Đã sao chép", $"Nội dung \"{text}\" đã được sao chép.", "OK");
    }
}
