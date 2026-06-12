namespace Collectary.Presentation.ViewModels;

/// <summary>Thrown when a field editor reports a value that must block the item from being saved.</summary>
public class FieldValidationException(string message) : Exception(message);
