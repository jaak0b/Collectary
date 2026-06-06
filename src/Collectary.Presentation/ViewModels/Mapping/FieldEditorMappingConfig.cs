using System.Collections;
using Mapster;

namespace Collectary.Presentation.ViewModels.Mapping;

public class FieldEditorMappingConfig
{
    public TypeAdapterConfig Build()
    {
        var orchestrationManaged = new HashSet<string>
        {
            "PresetId", "ParentListFieldDefinitionId", "SharedFieldId", "GroupId",
            "MaxLength", "Min", "Max", "DecimalPlaces"
        };

        var config = new TypeAdapterConfig();
        config.Default
            .IgnoreMember((member, side) =>
                side == MemberSide.Destination &&
                (orchestrationManaged.Contains(member.Name) || IsCollection(member.Type)))
            .RequireDestinationMemberSource(true);
        return config;
    }

    private bool IsCollection(Type type) =>
        type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
}
