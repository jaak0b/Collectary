using Collectary.Core.Domain;
using Collectary.UI.ViewModels.ListCells;

namespace Collectary.UI.DI;

public interface IListCellBuilder
{
    IReadOnlyList<ListCellViewModelBase?> Build(IReadOnlyList<FieldValue> values, IReadOnlyList<FieldDefinition> listFields);
    bool HasListCellViewModel(Type definitionType);
}
