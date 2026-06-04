using Collectary.Presentation.ViewModels.Mapping;
using MapsterMapper;

namespace Collectary.UI.Tests;

/// Builds a real FieldEditorMapper backed by the production Mapster config, for use in tests.
public class TestFieldEditorMapper
{
    public IFieldEditorMapper Create() =>
        new FieldEditorMapper(new Mapper(new FieldEditorMappingConfig().Build()));
}
