using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaDCT.Services;
using AvaloniaDCT.ViewModels;
using AvaloniaDCT.Views;

namespace AvaloniaDCT;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var profile = AppSecurityProfile.Detect();
            var database = new SqliteDatabase(profile);
            if (!profile.IsHardened)
            {
                database.Initialize();
            }

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginViewModel = new LoginViewModel(database);
            var loginWindow = new LoginWindow
            {
                DataContext = loginViewModel
            };

            var loggedIn = false;
            loginViewModel.LoginSucceeded += user =>
            {
                loggedIn = true;
                if (profile.IsHardened)
                {
                    database.Initialize();
                }

                var mainWindow = new MainWindow();
                var mainViewModel = new MainViewModel(
                    database,
                    user,
                    new DialogService(mainWindow),
                    new AvaloniaClipboardService(mainWindow));
                mainWindow.DataContext = mainViewModel;
                mainViewModel.ExitRequested += () => mainWindow.Close();
                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
                loginWindow.Close();
            };

            loginWindow.Closed += (_, _) =>
            {
                if (!loggedIn)
                {
                    desktop.Shutdown();
                }
            };

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
