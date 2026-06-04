using Collectary.Core.Domain;
using Collectary.Presentation.ViewModels.ListCells;

namespace Collectary.Presentation.DI;

public interface IListCellBuilder
{
    IReadOnlyList<ListCellViewModelBase?> Build(IReadOnlyList<FieldValue> values, IReadOnlyList<FieldDefinition> listFields);
    bool HasListCellViewModel(Type definitionType);
}
