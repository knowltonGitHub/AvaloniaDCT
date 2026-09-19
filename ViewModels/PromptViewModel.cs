using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaDCT.ViewModels;

public partial class PromptViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _nameLabel = "Name";

    [ObservableProperty]
    private string _name = "";

    [ObservableProperty]
    private bool _showName = true;

    [ObservableProperty]
    private string _valueLabel = "Value";

    [ObservableProperty]
    private string _value = "";

    [ObservableProperty]
    private bool _showValue;

    [ObservableProperty]
    private bool _valueIsMultiline;

    public event Action<bool>? CloseRequested;

    public PromptViewModel(PromptRequest request)
    {
        Title = request.Title;
        NameLabel = request.NameLabel;
        Name = request.Name;
        ShowName = request.ShowName;
        ValueLabel = request.ValueLabel;
        Value = request.Value;
        ShowValue = request.ShowValue;
        ValueIsMultiline = request.ValueIsMultiline;
    }

    [RelayCommand]
    private void Save()
    {
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(false);
    }
}
