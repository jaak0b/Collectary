namespace Collectary.Core.Domain;

/// <summary>How a field's label is positioned relative to its input in the item editor.</summary>
public enum FieldLabelLayout
{
    /// <summary>Label sits to the left of the input (compact).</summary>
    Beside,

    /// <summary>Label sits above the input (clean stacked columns).</summary>
    Above,

    /// <summary>Beside for single-column presets, above when the preset has multiple columns.</summary>
    Adaptive
}
