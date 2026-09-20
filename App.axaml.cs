using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AvaloniaDCT.Services;
using AvaloniaDCT.ViewModels;
using AvaloniaDCT.Views;

namespace AvaloniaDCT;

// Composition root: create services, show login, then main window.
// Views never new up SqliteDatabase themselves — that keeps MVVM testable.
public partial class App : Application
{
    // Load App.axaml (theme) before any window is shown.
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // Called when Avalonia is ready. This is where a desktop app creates its first Window.
    public override void OnFrameworkInitializationCompleted()
    {
        // IClassicDesktopStyleApplicationLifetime = Windows/Linux/macOS desktop (not a browser/mobile lifetime).
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Choose Debug plaintext vs encrypted AppData. Detect() has no file I/O side effects.
            var profile = AppSecurityProfile.Detect();
            var database = new SqliteDatabase(profile);
            if (!profile.IsHardened)
            {
                // Debug: open/create the exe-folder DB and seed knowlton before the login screen.
                database.Initialize();
            }

            // Do not quit when the login window closes if we are about to show MainWindow.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var loginViewModel = new LoginViewModel(database, profile);
            var loginWindow = new LoginWindow
            {
                // DataContext is how XAML {Binding ...} finds LoginViewModel properties.
                DataContext = loginViewModel
            };

            var loggedIn = false;
            loginViewModel.LoginSucceeded += user =>
            {
                loggedIn = true;
                if (profile.IsHardened)
                {
                    // Encrypted path: backups/schema only after the passphrase has opened the file.
                    database.Initialize();
                }

                var mainWindow = new MainWindow();
                var mainViewModel = new MainViewModel(
                    database,
                    user,
                    new DialogService(mainWindow),           // dialogs owned by MainWindow
                    new AvaloniaClipboardService(mainWindow), // OS clipboard via this window
                    profile);
                mainWindow.DataContext = mainViewModel;
                mainViewModel.ExitRequested += () => mainWindow.Close();
                desktop.MainWindow = mainWindow;
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose; // File > Exit / close = quit
                mainWindow.Show();
                loginWindow.Close();
            };

            loginWindow.Closed += (_, _) =>
            {
                if (!loggedIn)
                {
                    // User closed login without succeeding — end the process.
                    desktop.Shutdown();
                }
            };

            desktop.MainWindow = loginWindow;
            loginWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
