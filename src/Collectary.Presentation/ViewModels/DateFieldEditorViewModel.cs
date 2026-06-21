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

    [ObservableProperty]
    public partial TimeSpan? Time { get; set; }

    public bool WithTime => _definition.WithTime;

    public DateFieldEditorViewModel(DateFieldDefinition definition, DateFieldValue value)
    {
        _definition = definition;
        _fieldValue = value;
        Date = value.Value;
        Time = _definition.WithTime ? value.Value?.TimeOfDay : null;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) => Date = data.PastDateUtc();

    public override FieldValue GetCurrentValue()
    {
        if (!Date.HasValue)
        {
            _fieldValue.Value = null;
            return _fieldValue;
        }

        var composed = _definition.WithTime ? Date.Value.Date + (Time ?? TimeSpan.Zero) : Date.Value.Date;
        _fieldValue.Value = DateTime.SpecifyKind(composed, DateTimeKind.Utc);
        return _fieldValue;
    }
}
