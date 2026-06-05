using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class ProgressFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly ProgressFieldDefinition _definition;
    private readonly ProgressFieldValue _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fraction))]
    [NotifyPropertyChangedFor(nameof(Percent))]
    public partial int? Have { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Fraction))]
    [NotifyPropertyChangedFor(nameof(Percent))]
    public partial int? Total { get; set; }

    /// <summary>Completion as 0–1, clamped; 0 when there is no positive total.</summary>
    public double Fraction =>
        Total is > 0 ? Math.Clamp((Have ?? 0) / (double)Total.Value, 0, 1) : 0;

    public int Percent => (int)Math.Round(Fraction * 100);

    public ProgressFieldEditorViewModel(ProgressFieldDefinition definition, ProgressFieldValue value)
    {
        _definition = definition;
        _value = value;
        Have = value.Have;
        Total = value.Total;
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        _value.Have = Have;
        _value.Total = Total;
        return _value;
    }
}
