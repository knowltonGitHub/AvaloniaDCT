using System.Globalization; // CultureInfo for FormattedText
using Avalonia; // AvaloniaPropertyChangedEventArgs, PixelPoint
using Avalonia.Controls; // Window, Screens
using Avalonia.Media; // FormattedText, Typeface, Brushes, FlowDirection

namespace AvaloniaDCT.Views; // Code-behind for LoginWindow.axaml

// Keep this thin: layout/chrome only, no login logic.
public partial class LoginWindow : Window // partial: XAML compiler generates the other half
{
    public LoginWindow() // App.axaml.cs does `new LoginWindow { DataContext = ... }`
    {
        InitializeComponent(); // Load the XAML and wire named controls
        PropertyChanged += OnWindowPropertyChanged; // Title can change after DataContext is set
        Opened += (_, _) => FitWidthToTitle(recenter: true); // After show: measure caption and center
    } // end constructor

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) // Any Window property
    {
        if (e.Property == TitleProperty) // Caption string changed (TitleMode appended)
        {
            FitWidthToTitle(recenter: false); // Widen in place; do not jump the window
        } // end if Title changed
    } // end OnWindowPropertyChanged

    private void FitWidthToTitle(bool recenter) // SizeToContent measures client content, not the Windows caption
    {
        var title = Title; // Current caption including DEVELOPMENT / HARDENED / RELEASE
        if (string.IsNullOrWhiteSpace(title)) // No title yet
        {
            return; // Nothing to measure
        } // end if blank title

        var formatted = new FormattedText( // Measure the caption as Windows would draw it
            title, // The string
            CultureInfo.CurrentCulture, // User locale
            FlowDirection.LeftToRight, // English UI
            new Typeface("Segoe UI"), // Typical Windows caption font
            12, // Approximate caption font size
            Brushes.Black); // Color does not affect Width

        const double captionChromeWidth = 240; // Icon + min/max/close on Windows
        var needed = Math.Ceiling(formatted.Width + captionChromeWidth); // DIP width to show the whole title
        if (needed <= Width) // Already wide enough
        {
            return; // Do not shrink
        } // end if already wide

        Width = needed; // Grow the window
        if (recenter) // First Opened only
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary; // Monitor this window is on
            if (screen is null) // No screen info (rare)
            {
                return; // Leave Position alone
            } // end if no screen

            var area = screen.WorkingArea; // Desktop minus taskbar, in pixels
            var scaling = DesktopScaling; // DIP vs pixel on high-DPI displays
            Position = new PixelPoint( // Recenter after widening
                area.X + (int)((area.Width - Width * scaling) / 2), // Horizontal center
                area.Y + (int)((area.Height - Height * scaling) / 2)); // Vertical center
        } // end if recenter
    } // end FitWidthToTitle
} // end LoginWindow
