namespace Collectary.Core.Domain.Fields;

[AttributeUsage(AttributeTargets.Class)]
public sealed class FieldIconAttribute(string icon) : Attribute
{
    public string Icon { get; } = icon;
}
