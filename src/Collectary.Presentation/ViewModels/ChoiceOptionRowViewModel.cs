using CommunityToolkit.Mvvm.ComponentModel;

namespace Collectary.UI.ViewModels;

public partial class ChoiceOptionRowViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Value { get; set; }

    public ChoiceOptionRowViewModel(string value)
    {
        Value = value;
    }
}
