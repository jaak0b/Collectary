using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DateFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DateFieldDefinition _definition;
    private readonly DateFieldValue _fieldValue;

    [ObservableProperty]
    public partial DateTimeOffset? Date { get; set; }

    public DateFieldEditorViewModel(DateFieldDefinition definition, DateFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Date = value.Value.HasValue ? new DateTimeOffset(value.Value.Value, TimeSpan.Zero) : null;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Value = Date?.UtcDateTime;
        return _fieldValue;
    }
}
