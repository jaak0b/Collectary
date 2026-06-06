using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DateFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DateFieldDefinition _definition;
    private readonly DateFieldValue _fieldValue;

    [ObservableProperty]
    public partial DateTime? Date { get; set; }

    public DateFieldEditorViewModel(DateFieldDefinition definition, DateFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Date = value.Value;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Date = data.PastDateUtc();

    public override FieldValue GetCurrentValue()
    {
        _fieldValue.Value = Date.HasValue ? DateTime.SpecifyKind(Date.Value, DateTimeKind.Utc) : null;
        return _fieldValue;
    }
}
