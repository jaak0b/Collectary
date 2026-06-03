namespace Collectary.Core.Domain.Fields;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property)]
public sealed class LocalizedNameAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
