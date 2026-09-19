using AvaloniaDCT.Models;
using AvaloniaDCT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

public partial class PreferencesViewModel : ObservableObject
{
    private readonly SqliteDatabase _database;
    private readonly UserSession _user;

    [ObservableProperty]
    private string _fromAddress = "";

    [ObservableProperty]
    private string _host = "";

    [ObservableProperty]
    private string _port = "";

    [ObservableProperty]
    private string _userName = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _sourceIp = "";

    [ObservableProperty]
    private string _sourcePort = "";

    [ObservableProperty]
    private string _destinationIp = "";

    [ObservableProperty]
    private string _destinationPort = "";

    public event Action? CloseRequested;

    public PreferencesViewModel(SqliteDatabase database, UserSession user)
    {
        _database = database;
        _user = user;

        var preferences = database.LoadPreferences(user.UserId);
        FromAddress = preferences.FromAddress;
        Host = preferences.Host;
        Port = preferences.Port;
        UserName = preferences.UserName;
        Password = preferences.Password;
        SourceIp = preferences.SourceIp;
        SourcePort = preferences.SourcePort;
        DestinationIp = preferences.DestinationIp;
        DestinationPort = preferences.DestinationPort;
    }

    [RelayCommand]
    private void Save()
    {
        _database.SavePreferences(_user.UserId, new UserPreferences
        {
            FromAddress = FromAddress,
            Host = Host,
            Port = Port,
            UserName = UserName,
            Password = Password,
            SourceIp = SourceIp,
            SourcePort = SourcePort,
            DestinationIp = DestinationIp,
            DestinationPort = DestinationPort
        });

        CloseRequested?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke();
    }
}
