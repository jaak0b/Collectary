using Autofac.Features.Indexed;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.DI;

public class ListCellBuilder(IIndex<string, Func<FieldValue, FieldDefinition, ListCellViewModelBase>> factories) : IListCellBuilder
{
    public IReadOnlyList<ListCellViewModelBase?> Build(
        IReadOnlyList<FieldValue> values,
        IReadOnlyList<FieldDefinition> listFields) =>
        listFields
            .Where(field => field is not DisplayNameFieldDefinition)
            .Select(field =>
            {
                var fv = values.FirstOrDefault(v => v.FieldDefinitionId == field.Id);
                var key = field.GetType().Name;
                if (fv is null || !factories.TryGetValue(key, out var factory)) return null;
                return factory(fv, field);
            })
            .ToList();

    public bool HasListCellViewModel(Type definitionType) =>
        factories.TryGetValue(definitionType.Name, out _);
}
