using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DateRangeFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DateRangeFieldDefinition _definition;
    private readonly DateRangeFieldValue _value;

    [ObservableProperty]
    public partial DateTimeOffset? From { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? To { get; set; }

    public DateRangeFieldEditorViewModel(DateRangeFieldDefinition definition, DateRangeFieldValue value)
    {
        _definition = definition;
        _value = value;
        From = ToOffset(value.From);
        To = ToOffset(value.To);
    }

    private static DateTimeOffset? ToOffset(DateTime? d) =>
        d.HasValue ? new DateTimeOffset(d.Value, TimeSpan.Zero) : null;

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.From = From?.UtcDateTime;
        _value.To = To?.UtcDateTime;
        return _value;
    }
}
