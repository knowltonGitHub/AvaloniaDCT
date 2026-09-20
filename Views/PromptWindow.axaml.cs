using Avalonia.Controls; // Window, TextBox
using Avalonia.Threading; // Dispatcher.UIThread.Post

namespace AvaloniaDCT.Views; // Code-behind for PromptWindow.axaml

public partial class PromptWindow : Window // partial: XAML generates NameBox / ValueBox fields from x:Name
{
    public PromptWindow() // DialogService does `new PromptWindow { DataContext = ... }`
    {
        InitializeComponent(); // Load PromptWindow.axaml
        Opened += OnOpened; // Focus after the window is actually shown
    } // end constructor

    private void OnOpened(object? sender, EventArgs e) // First layout has completed
    {
        var box = NameBox.IsVisible ? NameBox : ValueBox; // Name for Add/Rename; value for Edit Value
        Dispatcher.UIThread.Post(() => // Run after Avalonia finishes laying out the dialog
        {
            box.Focus(); // Keyboard lands in the first visible field
            if (!string.IsNullOrEmpty(box.Text)) // Rename / Edit Value has existing text
            {
                box.SelectAll(); // Next keystroke replaces the old name/value
            } // end if has text
        }, DispatcherPriority.Input); // Input priority: after layout, before the user types
    } // end OnOpened
} // end PromptWindow
