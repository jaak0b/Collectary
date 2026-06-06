using Collectary.Core.Domain.Import;

namespace Collectary.Core.Ports;

public interface IGridShaper
{
    ShapedGrid Shape(IReadOnlyList<IReadOnlyList<WorkbookCell>> rows, bool transpose, bool firstRowIsHeader);
}
