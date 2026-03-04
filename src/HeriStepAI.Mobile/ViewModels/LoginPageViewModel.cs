using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;

namespace HeriStepAI.Mobile.ViewModels;

public partial class LoginPageViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string successMessage = "";
    [ObservableProperty] private bool hasSuccess;

    public LoginPageViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng nhập email và mật khẩu";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var (success, error) = await _authService.LoginAsync(Email.Trim(), Password);
        IsLoading = false;

        if (success)
        {
            Application.Current!.MainPage = IPlatformApplication.Current!.Services.GetRequiredService<AppShell>();
        }
        else
        {
            ErrorMessage = error;
            HasError = true;
        }
    }

    [RelayCommand]
    private void GoToRegister()
    {
        var registerPage = IPlatformApplication.Current!.Services.GetRequiredService<RegisterPage>();
        Application.Current!.MainPage = registerPage;
    }
}
