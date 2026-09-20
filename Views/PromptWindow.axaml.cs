using Avalonia.Controls;
using Avalonia.Threading;

namespace AvaloniaDCT.Views;

public partial class PromptWindow : Window
{
    public PromptWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var box = NameBox.IsVisible ? NameBox : ValueBox;
        // Post to Input so focus/select run after Avalonia finishes laying out the dialog.
        Dispatcher.UIThread.Post(() =>
        {
            box.Focus();
            if (!string.IsNullOrEmpty(box.Text))
            {
                box.SelectAll(); // Rename: next keystroke replaces the old name.
            }
        }, DispatcherPriority.Input);
    }
}
