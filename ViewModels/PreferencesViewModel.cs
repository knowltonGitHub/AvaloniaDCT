using AvaloniaDCT.Models; // UserSession, UserPreferences
using AvaloniaDCT.Services; // SqliteDatabase
using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject, ObservableProperty
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // Preferences window logic

// Loads/saves SMTP fields on the preferences table. LAN chat is on the Chat tab, not this form.
public partial class PreferencesViewModel : ObservableObject // partial for generated properties and commands
{
    private readonly SqliteDatabase _database; // Load/SavePreferences
    private readonly UserSession _user; // Whose row to load/save

    [ObservableProperty] // Generates FromAddress
    private string _fromAddress = ""; // SMTP From Address TextBox

    [ObservableProperty] // Generates Host
    private string _host = ""; // SMTP Host TextBox

    [ObservableProperty] // Generates Port
    private string _port = ""; // SMTP Port TextBox

    [ObservableProperty] // Generates UserName
    private string _userName = ""; // SMTP user name TextBox

    [ObservableProperty] // Generates Password
    private string _password = ""; // SMTP password TextBox

    public event Action? CloseRequested; // Save and Cancel both close; DialogService owns Close()

    public PreferencesViewModel(SqliteDatabase database, UserSession user) // DialogService constructs this
    {
        _database = database; // Keep for Save
        _user = user; // Keep for user.id

        var preferences = database.LoadPreferences(user.UserId); // Existing row or empty DTO
        FromAddress = preferences.FromAddress; // Fill SMTP From
        Host = preferences.Host; // Fill SMTP host
        Port = preferences.Port; // Fill SMTP port
        UserName = preferences.UserName; // Fill SMTP user
        Password = preferences.Password; // Fill SMTP password
    } // end constructor

    [RelayCommand] // Becomes SaveCommand
    private void Save() // Write the form back to SQLite
    {
        _database.SavePreferences(_user.UserId, new UserPreferences // Snapshot of SMTP fields
        {
            FromAddress = FromAddress, // SMTP From
            Host = Host, // SMTP host
            Port = Port, // SMTP port
            UserName = UserName, // SMTP user
            Password = Password // SMTP password
        });

        CloseRequested?.Invoke(); // Close after a successful save
    } // end Save

    [RelayCommand] // Becomes CancelCommand
    private void Cancel() // Discard edits
    {
        CloseRequested?.Invoke(); // Close without writing
    } // end Cancel
} // end PreferencesViewModel
