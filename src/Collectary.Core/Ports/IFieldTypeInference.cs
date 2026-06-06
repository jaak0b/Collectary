using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Import;

namespace Collectary.Core.Ports;

public interface IFieldTypeInference
{
    FieldDefinition Infer(IReadOnlyList<WorkbookCell> columnCells, CultureInfo culture);
}
