using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;
using HeriStepAI.Mobile.Views;

namespace HeriStepAI.Mobile.ViewModels;

public partial class RegisterPageViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string email = "";
    [ObservableProperty] private string password = "";
    [ObservableProperty] private string confirmPassword = "";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private bool hasError;

    public RegisterPageViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task Register()
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Vui lòng điền đầy đủ thông tin";
            HasError = true;
            return;
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp";
            HasError = true;
            return;
        }

        if (Password.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var (success, error) = await _authService.RegisterAsync(Email.Trim(), Password, FullName.Trim());
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
    private void GoBack()
    {
        var loginPage = IPlatformApplication.Current!.Services.GetRequiredService<LoginPage>();
        Application.Current!.MainPage = loginPage;
    }
}
