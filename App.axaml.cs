using Avalonia; // Application base class
using Avalonia.Controls; // Window, ShutdownMode
using Avalonia.Controls.ApplicationLifetimes; // IClassicDesktopStyleApplicationLifetime
using Avalonia.Markup.Xaml; // AvaloniaXamlLoader
using AvaloniaDCT.Services; // AppSecurityProfile, SqliteDatabase, DialogService, clipboard
using AvaloniaDCT.ViewModels; // LoginViewModel, MainViewModel
using AvaloniaDCT.Views; // LoginWindow, MainWindow

namespace AvaloniaDCT; // Same namespace as App.axaml's x:Class

// Composition root: create services, show login, then main window.
// Views never new up SqliteDatabase themselves — that keeps MVVM testable.
public partial class App : Application // partial: XAML compiler generates the other half
{
    public override void Initialize() // Load App.axaml (theme) before any window is shown
    {
        AvaloniaXamlLoader.Load(this); // Parse App.axaml and attach FluentTheme to this Application
    } // end Initialize

    public override void OnFrameworkInitializationCompleted() // Avalonia is ready; create the first Window
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) // Desktop OS (not browser/mobile)
        {
            var profile = AppSecurityProfile.Detect(); // Choose Debug plaintext vs encrypted AppData; no file I/O here
            var database = new SqliteDatabase(profile); // One DB helper for the whole process
            if (!profile.IsHardened) // Debug plaintext path
            {
                database.Initialize(); // Open/create the exe-folder DB and seed knowlton before the login screen
            } // end if not hardened

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown; // Do not quit when login closes if we are about to show MainWindow

            var loginViewModel = new LoginViewModel(database, profile); // Login screen logic (not the Window)
            var loginWindow = new LoginWindow // The actual login Window from Views/LoginWindow.axaml
            {
                DataContext = loginViewModel // XAML {Binding ...} reads LoginViewModel from DataContext
            };

            var loggedIn = false; // Closed-without-success vs closed-after-login
            loginViewModel.LoginSucceeded += user => // Fired when TryLogin returns a UserSession
            {
                loggedIn = true; // So loginWindow.Closed will not Shutdown() the process
                if (profile.IsHardened) // Encrypted path
                {
                    database.Initialize(); // Backups/schema only after the passphrase has opened the file
                } // end if hardened after login

                var mainWindow = new MainWindow(); // Empty window; DataContext is set next
                var mainViewModel = new MainViewModel( // Main screen logic
                    database, // Same SQLite helper as login
                    user, // Who just signed in
                    new DialogService(mainWindow), // Dialogs owned by MainWindow
                    new AvaloniaClipboardService(mainWindow), // OS clipboard via this window
                    profile); // Title bar mode string
                mainWindow.DataContext = mainViewModel; // Bind tags/items/commands to the window
                mainViewModel.ExitRequested += () => mainWindow.Close(); // File > Exit closes the window (which persists)
                desktop.MainWindow = mainWindow; // This is now the app's primary window
                desktop.ShutdownMode = ShutdownMode.OnMainWindowClose; // File > Exit / close caption = quit
                mainWindow.Show(); // Display the clipboard UI
                loginWindow.Close(); // Drop the login window now that main is up
            }; // end LoginSucceeded handler

            loginWindow.Closed += (_, _) => // User closed login with X or after success
            {
                if (!loggedIn) // Closed without a successful login
                {
                    desktop.Shutdown(); // End the process (OnExplicitShutdown would otherwise leave it running)
                } // end if not logged in
            }; // end login Closed handler

            desktop.MainWindow = loginWindow; // First window the desktop lifetime tracks
            loginWindow.Show(); // Display login (Create account or Sign in)
        } // end if desktop lifetime

        base.OnFrameworkInitializationCompleted(); // Let Avalonia finish its own startup
    } // end OnFrameworkInitializationCompleted
} // end App
