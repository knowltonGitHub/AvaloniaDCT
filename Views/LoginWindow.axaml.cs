using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaDCT.Views;

// Code-behind for LoginWindow.axaml. Keep this thin: layout/chrome only, no login logic.
public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent(); // Load the XAML and wire named controls.
        PropertyChanged += OnWindowPropertyChanged;
        Opened += (_, _) => FitWidthToTitle(recenter: true);
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TitleProperty)
        {
            FitWidthToTitle(recenter: false);
        }
    }

    // SizeToContent measures client content, not the Windows caption. Measure the title string
    // and widen so DEVELOPMENT / DEBUG HARDENED / RELEASE labels are fully visible.
    private void FitWidthToTitle(bool recenter)
    {
        var title = Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var formatted = new FormattedText(
            title,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            Brushes.Black);

        const double captionChromeWidth = 240; // Icon + min/max/close on Windows.
        var needed = Math.Ceiling(formatted.Width + captionChromeWidth);
        if (needed <= Width)
        {
            return;
        }

        Width = needed;
        if (recenter)
        {
            var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
            if (screen is null)
            {
                return;
            }

            var area = screen.WorkingArea;
            var scaling = DesktopScaling; // DIP vs pixel on high-DPI displays.
            Position = new PixelPoint(
                area.X + (int)((area.Width - Width * scaling) / 2),
                area.Y + (int)((area.Height - Height * scaling) / 2));
        }
    }
}
