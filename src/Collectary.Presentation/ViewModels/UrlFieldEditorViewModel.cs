using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class UrlFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly UrlFieldDefinition _definition;
    private readonly UrlFieldValue _fieldValue;

    [ObservableProperty]
    public partial string? Url { get; set; }

    public UrlFieldEditorViewModel(UrlFieldDefinition definition, UrlFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Url = value.Url;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Url = Url;
        return _fieldValue;
    }
}
