using System.Globalization;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;

namespace Collectary.Core.UseCases.Import;

public sealed class SpreadsheetImportService : ISpreadsheetImportService
{
    private readonly IItemUseCase _items;
    private readonly IPresetUseCase _presets;

    public SpreadsheetImportService(IItemUseCase items, IPresetUseCase presets)
    {
        _items = items;
        _presets = presets;
    }

    public async Task<ImportSummary> ImportExistingAsync(Guid presetId, ShapedGrid grid, IReadOnlyList<ColumnMapping> mappings, CultureInfo culture)
    {
        var effective = await _presets.GetEffectiveFieldsAsync(presetId);
        return await ImportRowsAsync(presetId, effective.Fields, grid, mappings, culture);
    }

    public async Task<ImportNewResult> ImportNewAsync(string presetName, ShapedGrid grid, IReadOnlyList<NewFieldColumn> columns, CultureInfo culture)
    {
        var preset = new Preset { Name = presetName };
        var title = new DisplayNameFieldDefinition { PresetId = preset.Id, DisplayOrder = 0 };
        preset.Fields.Add(title);

        var mappings = new List<ColumnMapping>();
        var order = 1;
        foreach (var column in columns)
        {
            if (column.IsTitle)
            {
                mappings.Add(new ColumnMapping(column.ColumnIndex, Guid.Empty, true));
                continue;
            }
            column.Definition.PresetId = preset.Id;
            column.Definition.DisplayOrder = order++;
            if (column.Definition is ITextImportable importable) importable.ApplyImportDefaults();
            preset.Fields.Add(column.Definition);
            mappings.Add(new ColumnMapping(column.ColumnIndex, column.Definition.Id, false));
        }

        await _presets.CreatePresetAsync(preset);
        var summary = await ImportRowsAsync(preset.Id, preset.Fields, grid, mappings, culture);
        return new ImportNewResult(preset, summary);
    }

    private async Task<ImportSummary> ImportRowsAsync(
        Guid presetId,
        IReadOnlyList<FieldDefinition> fields,
        ShapedGrid grid,
        IReadOnlyList<ColumnMapping> mappings,
        CultureInfo culture)
    {
        var fieldsById = fields.GroupBy(f => f.Id).ToDictionary(g => g.Key, g => g.First());
        var distinctMappings = DistinctMappings(mappings);
        var imported = 0;
        var skipped = new List<ImportIssue>();
        var warnings = new List<ImportIssue>();
        var duplicateNotices = new List<ImportIssue>();
        var seenUniqueValues = await BuildUniqueValueTrackersAsync(presetId, fieldsById, distinctMappings);

        for (var rowIndex = 0; rowIndex < grid.Rows.Count; rowIndex++)
        {
            var row = grid.Rows[rowIndex];
            var rowNumber = rowIndex + 1;
            var item = new Item { PresetId = presetId };
            var unparsed = new List<string>();
            var duplicates = new List<string>();
            var pendingUnique = new List<UniqueValueHit>();

            foreach (var mapping in distinctMappings)
            {
                if (mapping.ColumnIndex >= row.Count) continue;
                var cell = row[mapping.ColumnIndex];
                if (cell.Kind == WorkbookCellKind.Blank || string.IsNullOrWhiteSpace(cell.Text)) continue;

                if (mapping.IsTitle)
                {
                    item.DisplayName = cell.Text!.Trim();
                    continue;
                }

                if (!fieldsById.TryGetValue(mapping.FieldDefinitionId, out var definition)) continue;
                if (definition is not ITextImportable importable)
                {
                    unparsed.Add(definition.Label);
                    continue;
                }

                var cellCulture = cell.EffectiveCulture(culture);
                if (importable.TryImportFromText(cell.Text!, cellCulture, out var value))
                {
                    value.FieldDefinitionId = definition.Id;
                    item.Values.Add(value);

                    if (seenUniqueValues.TryGetValue(definition.Id, out var seen) && !value.IsEmpty)
                    {
                        var key = value.ToString()!;
                        if (seen.Contains(key)) duplicates.Add(definition.Label);
                        pendingUnique.Add(new UniqueValueHit(definition.Id, key));
                    }
                }
                else
                {
                    unparsed.Add($"{definition.Label}: '{cell.Text}'");
                }
            }

            if (string.IsNullOrWhiteSpace(item.DisplayName) && item.Values.Count == 0)
            {
                if (unparsed.Count > 0)
                    skipped.Add(new ImportIssue(rowNumber, ImportIssueKind.NoValues, string.Join("; ", unparsed)));
                continue;
            }

            try
            {
                await _items.CreateItemAsync(item);
                imported++;
                foreach (var hit in pendingUnique) seenUniqueValues[hit.FieldDefinitionId].Add(hit.Key);
                if (unparsed.Count > 0)
                    warnings.Add(new ImportIssue(rowNumber, ImportIssueKind.UnparsedCells, string.Join("; ", unparsed)));
                if (duplicates.Count > 0)
                    duplicateNotices.Add(new ImportIssue(rowNumber, ImportIssueKind.DuplicateValue, string.Join("; ", duplicates)));
            }
            catch (Exception ex)
            {
                skipped.Add(new ImportIssue(rowNumber, ImportIssueKind.Error, ex.Message));
            }
        }

        return new ImportSummary(imported, skipped, warnings, duplicateNotices);
    }

    private sealed record UniqueValueHit(Guid FieldDefinitionId, string Key);

    private async Task<Dictionary<Guid, HashSet<string>>> BuildUniqueValueTrackersAsync(
        Guid presetId,
        IReadOnlyDictionary<Guid, FieldDefinition> fieldsById,
        IReadOnlyList<ColumnMapping> distinctMappings)
    {
        var trackers = new Dictionary<Guid, HashSet<string>>();
        foreach (var mapping in distinctMappings)
        {
            if (mapping.IsTitle) continue;
            if (fieldsById.TryGetValue(mapping.FieldDefinitionId, out var definition)
                && definition is ITextImportable { EnforcesUniqueImportValues: true })
                trackers[mapping.FieldDefinitionId] = new HashSet<string>(StringComparer.Ordinal);
        }
        if (trackers.Count == 0) return trackers;

        foreach (var existing in await _items.GetItemsForPresetAsync(presetId))
            foreach (var value in existing.Values)
                if (!value.IsEmpty && trackers.TryGetValue(value.FieldDefinitionId, out var seen))
                    seen.Add(value.ToString()!);

        return trackers;
    }

    private IReadOnlyList<ColumnMapping> DistinctMappings(IReadOnlyList<ColumnMapping> mappings)
    {
        var result = new List<ColumnMapping>(mappings.Count);
        var seenFields = new HashSet<Guid>();
        var titleSeen = false;
        foreach (var mapping in mappings)
        {
            if (mapping.IsTitle)
            {
                if (titleSeen) continue;
                titleSeen = true;
            }
            else if (!seenFields.Add(mapping.FieldDefinitionId))
            {
                continue;
            }
            result.Add(mapping);
        }
        return result;
    }
}
