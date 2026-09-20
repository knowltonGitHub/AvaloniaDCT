using AvaloniaDCT.Models; // UserSession
using AvaloniaDCT.Services; // SqliteDatabase, AppSecurityProfile
using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject, ObservableProperty
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // Login screen logic

// `partial` because [ObservableProperty] / [RelayCommand] generate UserName, Password, LoginCommand, etc. in another file at compile time.
public partial class LoginViewModel : ObservableObject // ObservableObject implements INotifyPropertyChanged
{
    private readonly SqliteDatabase _database; // TryLogin target

    [ObservableProperty] // Generates public string UserName { get; set; } that notifies the TextBox
    private string _userName = // Backing field for UserName
#if DEBUG // F5 convenience
        "knowlton"; // Prefill Debug login; not used in Release/installer
#else // Release / installer
        ""; // Empty so the user types their own name
#endif // End UserName default

    [ObservableProperty] // Generates public string Password
    private string _password = // Backing field for Password
#if DEBUG // F5 convenience
        "knowlton"; // Prefill Debug password
#else // Release / installer
        ""; // Empty so the user types their own password
#endif // End Password default

    [ObservableProperty] // Generates public string ErrorMessage
    private string _errorMessage = ""; // Red text under the password box

    [ObservableProperty] // Generates public string WindowTitle
    private string _windowTitle = "Desktop Clipboard Toolkit - Login"; // Caption before Detect() fills TitleMode

    [ObservableProperty] // Generates public string Instruction
    private string _instruction = "Sign in to continue."; // Body text; first-run replaces this

    [ObservableProperty] // Generates public string LoginButtonText
    private string _loginButtonText = "Login"; // Button label; first-run becomes "Create account"

    public event Action<UserSession>? LoginSucceeded; // App.axaml.cs subscribes and opens MainWindow

    public LoginViewModel(SqliteDatabase database, AppSecurityProfile profile) // App.axaml.cs constructs this
    {
        _database = database; // Keep for Login()
        WindowTitle = $"Desktop Clipboard Toolkit - Login - {profile.TitleMode}"; // Show DEVELOPMENT / HARDENED / RELEASE

        var firstHardenedRun = profile.IsHardened && !File.Exists(profile.DatabasePath); // Encrypted and no CBDB.db yet
        if (firstHardenedRun) // First install of the installer / Release / DCT_HARDENED=1
        {
            Instruction = // Explain that this password is the encryption key
                "First launch: choose a user name and password. That creates the encrypted database. The password is the encryption key — remember it.";
            LoginButtonText = "Create account"; // Same command, different label
        } // end if first hardened run
    } // end constructor

    [RelayCommand] // Becomes LoginCommand on the generated public API; XAML binds to that
    private void Login() // Enter key and the Login / Create account button
    {
        ErrorMessage = ""; // Clear the last failure
        try // File permission / unexpected SQLite errors become ErrorMessage
        {
            var user = _database.TryLogin(UserName.Trim(), Password); // Hardened or plaintext login
            if (user is null) // Bad name/password or wrong SQLCipher key
            {
                ErrorMessage = "Invalid username or password."; // Shown in red on the form
                return; // Stay on the login window
            } // end if invalid

            LoginSucceeded?.Invoke(user); // App.axaml.cs shows MainWindow
        } // end try
        catch (Exception ex) // Unexpected I/O
        {
            ErrorMessage = ex.Message; // Show the exception text instead of crashing
        } // end catch
    } // end Login
} // end LoginViewModel
