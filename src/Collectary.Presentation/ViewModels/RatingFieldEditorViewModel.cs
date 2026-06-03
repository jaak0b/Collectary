using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.UI.ViewModels;

public partial class RatingFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly RatingFieldDefinition _definition;
    private readonly RatingFieldValue _fieldValue;

    [ObservableProperty]
    public partial int Stars { get; set; }

    public int MaxStars => _definition.MaxStars;

    public RatingFieldEditorViewModel(RatingFieldDefinition definition, RatingFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Stars = value.Stars ?? 0;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Stars = Stars > 0 ? Stars : null;
        return _fieldValue;
    }
}
