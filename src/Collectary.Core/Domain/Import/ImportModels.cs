namespace Collectary.Core.Domain.Import;

public sealed record ColumnMapping(int ColumnIndex, Guid FieldDefinitionId, bool IsTitle);

public sealed record NewFieldColumn(int ColumnIndex, FieldDefinition Definition, bool IsTitle);

public enum ImportIssueKind
{
    NoValues,
    UnparsedCells,
    Error
}

public sealed record ImportIssue(int RowNumber, ImportIssueKind Kind, string Detail);

public sealed record ImportSummary(int Imported, IReadOnlyList<ImportIssue> Skipped, IReadOnlyList<ImportIssue> Warnings);

public sealed record ImportNewResult(Preset Preset, ImportSummary Summary);
