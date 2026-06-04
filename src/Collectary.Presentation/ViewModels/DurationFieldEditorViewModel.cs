using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DurationFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DurationFieldDefinition _definition;
    private readonly DurationFieldValue _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValue))]
    public partial int? Hours { get; set; }

    [ObservableProperty]
    public partial int? Minutes { get; set; }

    public bool HasValue => Hours is not null || Minutes is not null;

    public DurationFieldEditorViewModel(DurationFieldDefinition definition, DurationFieldValue value)
    {
        _definition = definition;
        _value = value;
        if (value.TotalMinutes.HasValue)
        {
            Hours = value.TotalMinutes.Value / 60;
            Minutes = value.TotalMinutes.Value % 60;
        }
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        var h = Hours ?? 0;
        var m = Minutes ?? 0;
        _value.TotalMinutes = (h == 0 && m == 0) ? null : h * 60 + m;
        return _value;
    }
}
