using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels;

/// <summary>A selectable field-label-layout choice. A null <see cref="Value"/> means "inherit the global default".</summary>
public record FieldLabelLayoutOption(FieldLabelLayout? Value, string Display);
