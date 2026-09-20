using Avalonia;
using System;

namespace AvaloniaDCT;

// Process entry point. Avalonia is not a WinForms Application.Run(new Form()) app:
// you build an AppBuilder, then start a desktop lifetime that owns the message loop.
class Program
{
    // STAThread is required on Windows so COM (clipboard, some shell verbs) works.
    // Do not call Avalonia or UI APIs before StartWithClassicDesktopLifetime:
    // the framework is not initialized yet.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Shared by the running app and the Avalonia XAML designer / previewer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()          // Use our App class (App.axaml / App.axaml.cs).
            .UsePlatformDetect()                 // Win32 / macOS / Linux from one binary.
#if DEBUG
            .WithDeveloperTools()                // Avalonia DevTools (F12-style inspection) in Debug only.
#endif
            .WithInterFont()                     // Bundle Inter so the UI looks the same on every PC.
            .LogToTrace();                       // Diagnostic log to Trace (Visual Studio Output window).
}
