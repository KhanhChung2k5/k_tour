using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainPage] Error: {ex}");
            Content = new VerticalStackLayout
            {
                Padding = 20,
                Children = { new Label { Text = $"Lỗi MainPage: {ex.Message}", TextColor = Colors.Red } }
            };
        }
    }
}
