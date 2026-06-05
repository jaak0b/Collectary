using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Auth;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthService _auth;
    private readonly IAccountBootstrapper _bootstrapper;
    private readonly Action _onAuthenticated;

    [ObservableProperty]
    public partial string Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsRegisterMode { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string Title => LocalizationService.Instance[IsRegisterMode ? "Login_RegisterTitle" : "Login_Title"];
    public string SubmitLabel => LocalizationService.Instance[IsRegisterMode ? "Login_RegisterButton" : "Login_SignInButton"];
    public string ToggleLabel => LocalizationService.Instance[IsRegisterMode ? "Login_SwitchToLogin" : "Login_SwitchToRegister"];

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    partial void OnIsRegisterModeChanged(bool value)
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SubmitLabel));
        OnPropertyChanged(nameof(ToggleLabel));
    }

    public LoginViewModel(IAuthService auth, IAccountBootstrapper bootstrapper, Action onAuthenticated)
    {
        _auth = auth;
        _bootstrapper = bootstrapper;
        _onAuthenticated = onAuthenticated;
    }

    [RelayCommand]
    private void ToggleMode()
    {
        IsRegisterMode = !IsRegisterMode;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task Submit()
    {
        if (IsBusy) return;
        ErrorMessage = null;
        IsBusy = true;
        try
        {
            var user = IsRegisterMode
                ? await _auth.RegisterAsync(Username.Trim(), DisplayName.Trim(), Password, Email)
                : await _auth.LoginAsync(Username.Trim(), Password);

            if (user is null)
            {
                ErrorMessage = LocalizationService.Instance["Login_Failed"];
                return;
            }

            await _bootstrapper.BackfillOwnerlessAsync(user.Id);
            _onAuthenticated();
        }
        catch (UsernameTakenException)
        {
            ErrorMessage = LocalizationService.Instance["Login_UsernameTaken"];
        }
        catch (ArgumentException ex)
        {
            ErrorMessage = LocalizationService.Instance[
                ex.ParamName == "email" ? "Login_InvalidEmail" : "Login_FieldsRequired"];
        }
        finally
        {
            IsBusy = false;
        }
    }
}
