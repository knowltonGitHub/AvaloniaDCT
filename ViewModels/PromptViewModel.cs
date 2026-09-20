using CommunityToolkit.Mvvm.ComponentModel; // ObservableObject, ObservableProperty
using CommunityToolkit.Mvvm.Input; // RelayCommand

namespace AvaloniaDCT.ViewModels; // Shared dialog for Add Tag, Rename Tag, Add Item, Rename Item, Edit Value

public partial class PromptViewModel : ObservableObject // partial for generated properties and commands
{
    [ObservableProperty] // Generates Title
    private string _title = ""; // Window caption

    [ObservableProperty] // Generates NameLabel
    private string _nameLabel = "Name"; // Label above the name box

    [ObservableProperty] // Generates Name
    private string _name = ""; // Two-way bound to NameBox

    [ObservableProperty] // Generates ShowName
    private bool _showName = true; // Hides name controls for Edit Value

    [ObservableProperty] // Generates ValueLabel
    private string _valueLabel = "Value"; // Label above the value box

    [ObservableProperty] // Generates Value
    private string _value = ""; // Two-way bound to ValueBox

    [ObservableProperty] // Generates ShowValue
    private bool _showValue; // Hides value controls for tag-only dialogs

    [ObservableProperty] // Generates ValueIsMultiline
    private bool _valueIsMultiline; // AcceptsReturn on ValueBox

    public event Action<bool>? CloseRequested; // true = Save, false = Cancel; DialogService closes the window

    public PromptViewModel(PromptRequest request) // Copy the snapshot onto bindable properties
    {
        Title = request.Title; // Caption
        NameLabel = request.NameLabel; // Name label
        Name = request.Name; // Pre-fill (Rename)
        ShowName = request.ShowName; // Visibility
        ValueLabel = request.ValueLabel; // Value label
        Value = request.Value; // Pre-fill (Edit Value)
        ShowValue = request.ShowValue; // Visibility
        ValueIsMultiline = request.ValueIsMultiline; // Multiline value box
    } // end constructor

    [RelayCommand] // Becomes SaveCommand
    private void Save() // Save button
    {
        CloseRequested?.Invoke(true); // DialogService reads Name/Value after this
    } // end Save

    [RelayCommand] // Becomes CancelCommand
    private void Cancel() // Cancel button or Escape
    {
        CloseRequested?.Invoke(false); // DialogService returns null
    } // end Cancel
} // end PromptViewModel
