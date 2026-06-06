using System.Globalization;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class DurationFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly DurationFieldDefinition _definition;
    private readonly DurationFieldValue _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string Text { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(Text) && Parse(Text) is null;

    public DurationFieldEditorViewModel(DurationFieldDefinition definition, DurationFieldValue value)
    {
        _definition = definition;
        _value = value;
        Text = Format(value.TotalMinutes);
    }

    public override FieldDefinition Definition => _definition;

    public override FieldValue GetCurrentValue()
    {
        var parsed = Parse(Text);
        _value.TotalMinutes = parsed is null or 0 ? null : parsed;
        return _value;
    }

    private string Format(int? totalMinutes)
    {
        if (totalMinutes is null) return string.Empty;
        var h = totalMinutes.Value / 60;
        var m = totalMinutes.Value % 60;
        if (h > 0 && m > 0) return $"{h}h {m}m";
        if (h > 0) return $"{h}h";
        return $"{m}m";
    }

    private int? Parse(string text)
    {
        var t = text.Trim().ToLowerInvariant();
        if (t.Length == 0) return null;

        if (int.TryParse(t, NumberStyles.None, CultureInfo.InvariantCulture, out var plain))
            return plain;

        var colon = Regex.Match(t, @"^(\d+):([0-5]?\d)$");
        if (colon.Success)
            return int.Parse(colon.Groups[1].Value) * 60 + int.Parse(colon.Groups[2].Value);

        var hm = Regex.Match(t, @"^(?:(\d+)\s*h)?\s*(?:(\d+)\s*m)?$");
        if (hm.Success && (hm.Groups[1].Success || hm.Groups[2].Success))
        {
            var h = hm.Groups[1].Success ? int.Parse(hm.Groups[1].Value) : 0;
            var m = hm.Groups[2].Success ? int.Parse(hm.Groups[2].Value) : 0;
            return h * 60 + m;
        }

        return null;
    }
}
