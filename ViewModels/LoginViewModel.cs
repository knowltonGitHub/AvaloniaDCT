using AvaloniaDCT.Models;
using AvaloniaDCT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly SqliteDatabase _database;

    [ObservableProperty]
    private string _userName =
#if DEBUG
        "knowlton";
#else
        "";
#endif

    [ObservableProperty]
    private string _password =
#if DEBUG
        "knowlton";
#else
        "";
#endif

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private string _windowTitle = "Desktop Clipboard Toolkit - Login";

    [ObservableProperty]
    private string _instruction = "Sign in to continue.";

    [ObservableProperty]
    private string _loginButtonText = "Login";

    public event Action<UserSession>? LoginSucceeded;

    public LoginViewModel(SqliteDatabase database, AppSecurityProfile profile)
    {
        _database = database;
        WindowTitle = $"Desktop Clipboard Toolkit - Login - {profile.TitleMode}";

        var firstHardenedRun = profile.IsHardened && !File.Exists(profile.DatabasePath);
        if (firstHardenedRun)
        {
            Instruction =
                "First launch: choose a user name and password. That creates the encrypted database. The password is the encryption key — remember it.";
            LoginButtonText = "Create account";
        }
    }

    [RelayCommand]
    private void Login()
    {
        ErrorMessage = "";
        try
        {
            var user = _database.TryLogin(UserName.Trim(), Password);
            if (user is null)
            {
                ErrorMessage = "Invalid username or password.";
                return;
            }

            LoginSucceeded?.Invoke(user);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
