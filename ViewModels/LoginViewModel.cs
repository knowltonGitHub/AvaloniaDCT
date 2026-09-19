using AvaloniaDCT.Models;
using AvaloniaDCT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly SqliteDatabase _database;

    [ObservableProperty]
    private string _userName = "knowlton";

    [ObservableProperty]
    private string _password = "knowlton";

    [ObservableProperty]
    private string _errorMessage = "";

    public event Action<UserSession>? LoginSucceeded;

    public LoginViewModel(SqliteDatabase database)
    {
        _database = database;
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
