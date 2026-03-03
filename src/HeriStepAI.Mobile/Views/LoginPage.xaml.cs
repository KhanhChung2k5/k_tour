using HeriStepAI.Mobile.ViewModels;

namespace HeriStepAI.Mobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
