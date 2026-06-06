using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DateRangeFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DateRangeFieldDefinition _definition;
    private readonly DateRangeFieldValue _value;

    [ObservableProperty]
    public partial DateTime? From { get; set; }

    [ObservableProperty]
    public partial DateTime? To { get; set; }

    public DateRangeFieldEditorViewModel(DateRangeFieldDefinition definition, DateRangeFieldValue value)
    {
        _definition = definition;
        _value = value;
        From = value.From;
        To = value.To;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data)
    {
        From = data.PastDateUtc();
        To = From.Value.AddDays(data.Int(1, 30));
    }

    public override FieldValue GetCurrentValue()
    {
        _value.From = AsUtc(From);
        _value.To = AsUtc(To);
        return _value;
    }

    private DateTime? AsUtc(DateTime? d) =>
        d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null;
}
