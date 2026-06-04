using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests;

/// <summary>
/// Test helpers that add a field of a given type through the data-driven catalog command, replacing the
/// old per-type Add*FieldCommand surface.
/// </summary>
internal static class FieldListEditorTestExtensions
{
    private static FieldTypeCatalogEntry Entry<T>(this FieldListEditorViewModel vm) where T : FieldDefinition =>
        vm.AddableFieldTypes.First(e => e.Type == typeof(T));

    public static Task AddFieldAsync<T>(this FieldListEditorViewModel vm) where T : FieldDefinition =>
        vm.AddFieldOfTypeCommand.ExecuteAsync(vm.Entry<T>());

    public static void AddField<T>(this FieldListEditorViewModel vm) where T : FieldDefinition =>
        vm.AddFieldOfTypeCommand.Execute(vm.Entry<T>());
}
