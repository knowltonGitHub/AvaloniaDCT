using Avalonia; // AppBuilder, Application, and other Avalonia core types
using System; // STAThread and basic runtime types

namespace AvaloniaDCT; // Root namespace; matches RootNamespace in the csproj

// Process entry. Avalonia is not WinForms Application.Run(new Form()):
// you configure an AppBuilder, then start a desktop lifetime that owns the message loop.
class Program
{
    [STAThread] // Windows COM (clipboard, some shell verbs) requires a single-threaded apartment
    public static void Main(string[] args) => BuildAvaloniaApp() // OS calls this first
        .StartWithClassicDesktopLifetime(args); // Create the desktop message loop (windows, mouse, keys)

    public static AppBuilder BuildAvaloniaApp() // Shared by the running exe and the Avalonia XAML designer
        => AppBuilder.Configure<App>() // Use our App class (App.axaml + App.axaml.cs)
            .UsePlatformDetect() // Pick Win32 / macOS / Linux from one binary
#if DEBUG // This extra chain runs only in Debug builds
            .WithDeveloperTools() // Avalonia visual-tree inspector; stripped from Release
#endif // End Debug-only DevTools
            .WithInterFont() // Bundle the Inter font so the UI looks the same on every PC
            .LogToTrace(); // Write Avalonia diagnostics to Trace (Visual Studio Output window)
} // end Program
