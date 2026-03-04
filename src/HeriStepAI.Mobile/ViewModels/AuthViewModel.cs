using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HeriStepAI.Mobile.Services;

namespace HeriStepAI.Mobile.ViewModels;

public partial class AuthViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    // --- View toggle ---
    [ObservableProperty] private bool showLogin = true;
    [ObservableProperty] private bool showRegister = false;

    // --- Shared state ---
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private bool hasError;

    // --- Login fields ---
    [ObservableProperty] private string loginEmail = "";
    [ObservableProperty] private string loginPassword = "";

    // --- Register fields ---
    [ObservableProperty] private string fullName = "";
    [ObservableProperty] private string regEmail = "";
    [ObservableProperty] private string regPassword = "";
    [ObservableProperty] private string confirmPassword = "";

    public AuthViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private void GoToRegister()
    {
        HasError = false;
        ErrorMessage = "";
        ShowLogin = false;
        ShowRegister = true;
    }

    [RelayCommand]
    private void GoToLogin()
    {
        HasError = false;
        ErrorMessage = "";
        ShowRegister = false;
        ShowLogin = true;
    }

    [RelayCommand]
    private async Task Login()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            ErrorMessage = "Vui lòng nhập email và mật khẩu";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var (success, error) = await _authService.LoginAsync(LoginEmail.Trim(), LoginPassword);
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
    private async Task Register()
    {
        if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(RegEmail) || string.IsNullOrWhiteSpace(RegPassword))
        {
            ErrorMessage = "Vui lòng điền đầy đủ thông tin";
            HasError = true;
            return;
        }

        if (RegPassword != ConfirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp";
            HasError = true;
            return;
        }

        if (RegPassword.Length < 6)
        {
            ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        var (success, error) = await _authService.RegisterAsync(RegEmail.Trim(), RegPassword, FullName.Trim());
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
}
