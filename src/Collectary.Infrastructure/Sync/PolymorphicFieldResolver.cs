using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Collectary.Core.Domain;

namespace Collectary.Infrastructure.Sync;

public class PolymorphicFieldResolver : DefaultJsonTypeInfoResolver
{
    private readonly IReadOnlyList<Type> _fieldDefinitionSubtypes;
    private readonly IReadOnlyList<Type> _fieldValueSubtypes;

    public PolymorphicFieldResolver()
    {
        _fieldDefinitionSubtypes = DiscoverSubtypes(typeof(FieldDefinition));
        _fieldValueSubtypes = DiscoverSubtypes(typeof(FieldValue));
    }

    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = base.GetTypeInfo(type, options);

        for (var i = info.Properties.Count - 1; i >= 0; i--)
        {
            var property = info.Properties[i];
            var drop = property.PropertyType == typeof(Type)
                || (typeof(FieldValue).IsAssignableFrom(type) && property.Name == "Definition")
                || ((type == typeof(PresetSharedField) || type == typeof(ListSharedField)) && property.Name == "SharedField");
            if (drop) info.Properties.RemoveAt(i);
        }

        if (type == typeof(FieldDefinition))
            ApplyPolymorphism(info, _fieldDefinitionSubtypes);
        else if (type == typeof(FieldValue))
            ApplyPolymorphism(info, _fieldValueSubtypes);

        return info;
    }

    private void ApplyPolymorphism(JsonTypeInfo info, IReadOnlyList<Type> subtypes)
    {
        info.PolymorphismOptions = new JsonPolymorphismOptions
        {
            TypeDiscriminatorPropertyName = "$type",
            UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
        };
        foreach (var subtype in subtypes)
            info.PolymorphismOptions.DerivedTypes.Add(new JsonDerivedType(subtype, subtype.Name));
    }

    private IReadOnlyList<Type> DiscoverSubtypes(Type baseType) =>
        baseType.Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && baseType.IsAssignableFrom(t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
}
