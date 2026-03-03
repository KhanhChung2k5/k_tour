using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
