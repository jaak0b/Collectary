using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class CustomColorSlot : ObservableObject
{
    private readonly Action<CustomColorSlot> _onChanged;
    private readonly bool _initialized;
    private bool _suppress;

    public string Key { get; }
    public string LabelKey { get; }
    public string Label => LocalizationService.Instance[LabelKey];
    public bool IsEasy { get; }
    public bool IsOverridden { get; private set; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    [ObservableProperty]
    public partial bool IsRowVisible { get; set; }

    public CustomColorSlot(string key, string labelKey, bool isEasy, Color color, bool isOverridden,
        Action<CustomColorSlot> onChanged)
    {
        Key = key;
        LabelKey = labelKey;
        IsEasy = isEasy;
        _onChanged = onChanged;
        Color = color;
        IsOverridden = isOverridden;
        _initialized = true;
    }

    /// <summary>Sets the displayed color without marking the slot as overridden or notifying the owner.</summary>
    public void Revert(Color color)
    {
        _suppress = true;
        Color = color;
        IsOverridden = false;
        _suppress = false;
    }

    partial void OnColorChanged(Color value)
    {
        if (_suppress || !_initialized) return;
        IsOverridden = true;
        _onChanged(this);
    }
}
