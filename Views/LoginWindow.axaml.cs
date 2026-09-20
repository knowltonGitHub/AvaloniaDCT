using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaDCT.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
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

        // Caption icon + min/max/close buttons. SizeToContent only measures
        // client content, not the OS title bar.
        const double captionChromeWidth = 240;
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
            var scaling = DesktopScaling;
            Position = new PixelPoint(
                area.X + (int)((area.Width - Width * scaling) / 2),
                area.Y + (int)((area.Height - Height * scaling) / 2));
        }
    }
}
