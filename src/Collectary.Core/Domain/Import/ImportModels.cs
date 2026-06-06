namespace Collectary.Core.Domain.Import;

public sealed record ColumnMapping(int ColumnIndex, Guid FieldDefinitionId, bool IsTitle);

public sealed record NewFieldColumn(int ColumnIndex, FieldDefinition Definition, bool IsTitle);

public sealed record ImportIssue(int RowNumber, string Reason);

public sealed record ImportSummary(int Imported, IReadOnlyList<ImportIssue> Skipped, IReadOnlyList<ImportIssue> Warnings);
