using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class TimeFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly TimeFieldDefinition _definition;
    private readonly TimeFieldValue _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string Text { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(Text) && Parse(Text) is null;

    public TimeFieldEditorViewModel(TimeFieldDefinition definition, TimeFieldValue value)
    {
        _definition = definition;
        _value = value;
        Text = value.Value ?? string.Empty;
    }

    public override FieldDefinition Definition => _definition;

    public override void Randomize(Services.ISampleData data) =>
        Text = $"{data.Int(0, 23):D2}:{data.Int(0, 59):D2}";

    public override FieldValue GetCurrentValue()
    {
        var parsed = Parse(Text);
        _value.Value = parsed is null ? null : $"{(int)parsed.Value.TotalHours:D2}:{parsed.Value.Minutes:D2}";
        return _value;
    }

    private TimeSpan? Parse(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.Length == 0) return null;
        return TimeSpan.TryParseExact(trimmed, new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out var ts)
               && ts < TimeSpan.FromDays(1)
            ? ts
            : null;
    }
}
