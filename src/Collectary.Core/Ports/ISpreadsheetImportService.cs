using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Import;

namespace Collectary.Core.Ports;

public interface ISpreadsheetImportService
{
    Task<ImportSummary> ImportExistingAsync(Guid presetId, ShapedGrid grid, IReadOnlyList<ColumnMapping> mappings, CultureInfo culture);

    Task<(Preset Preset, ImportSummary Summary)> ImportNewAsync(string presetName, ShapedGrid grid, IReadOnlyList<NewFieldColumn> columns, CultureInfo culture);
}
