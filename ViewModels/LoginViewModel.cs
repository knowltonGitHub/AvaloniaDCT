using AvaloniaDCT.Models;
using AvaloniaDCT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

// Login screen logic. `partial` because [ObservableProperty] / [RelayCommand] generate
// UserName, Password, LoginCommand, etc. in another file at compile time (CommunityToolkit.Mvvm).
public partial class LoginViewModel : ObservableObject
{
    private readonly SqliteDatabase _database;

    [ObservableProperty]
    private string _userName =
#if DEBUG
        "knowlton"; // F5 convenience — not used in Release/installer.
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

    // App.axaml.cs subscribes and opens MainWindow when this fires.
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

    [RelayCommand] // Becomes LoginCommand on the generated public API; XAML binds to that.
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
            ErrorMessage = ex.Message; // File permission / unexpected SQLite errors.
        }
    }
}
