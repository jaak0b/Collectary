using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class TimeFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly TimeFieldDefinition _definition;
    private readonly TimeFieldValue _value;

    [ObservableProperty]
    public partial int? Hour { get; set; }

    [ObservableProperty]
    public partial int? Minute { get; set; }

    public TimeFieldEditorViewModel(TimeFieldDefinition definition, TimeFieldValue value)
    {
        _definition = definition;
        _value = value;
        if (TimeSpan.TryParseExact(value.Value, @"hh\:mm", null, out var ts))
        {
            Hour = ts.Hours;
            Minute = ts.Minutes;
        }
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Value = (Hour is null && Minute is null)
            ? null
            : $"{Hour ?? 0:D2}:{Minute ?? 0:D2}";
        return _value;
    }
}
