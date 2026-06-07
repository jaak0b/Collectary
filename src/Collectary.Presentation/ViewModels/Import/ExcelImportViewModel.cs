using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Domain.Import;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels.Import;

public enum ImportStep
{
    Sheet,
    Preview,
    Target,
    Map,
    Result
}

public partial class ExcelImportViewModel : ViewModelBase
{
    private readonly WorkbookData _data;
    private readonly IGridShaper _gridShaper;
    private readonly ICultureDetector _cultureDetector;
    private readonly IFieldTypeInference _inference;
    private readonly ISpreadsheetImportService _importService;
    private readonly IPresetUseCase _presetUseCase;
    private readonly IDialogService _dialogService;
    private readonly Func<Preset, Task>? _onFinished;
    private readonly Action _onClose;
    private readonly IReadOnlyList<FieldTypeChoice> _importableTypes;

    private ShapedGrid _shaped = new([], []);
    private Preset? _importedPreset;
    private readonly ImportStep _firstStep;

    public ExcelImportViewModel(
        WorkbookData data,
        IGridShaper gridShaper,
        ICultureDetector cultureDetector,
        IFieldTypeInference inference,
        ISpreadsheetImportService importService,
        IPresetUseCase presetUseCase,
        IDialogService dialogService,
        IReadOnlyList<Preset> existingPresets,
        Func<Preset, Task>? onFinished,
        Action onClose)
    {
        _data = data;
        _gridShaper = gridShaper;
        _cultureDetector = cultureDetector;
        _inference = inference;
        _importService = importService;
        _presetUseCase = presetUseCase;
        _dialogService = dialogService;
        _onFinished = onFinished;
        _onClose = onClose;

        _importableTypes = BuildImportableTypeChoices();

        foreach (var sheet in data.Sheets)
            SheetNames.Add(sheet.Name);
        foreach (var preset in existingPresets)
            ExistingPresets.Add(preset);

        AvailableCultures = BuildAvailableCultures();
        SourceCulture = AvailableCultures[0];

        SelectedSheetName = SheetNames.FirstOrDefault();
        SelectedPreset = ExistingPresets.FirstOrDefault();
        CreateNewCollection = ExistingPresets.Count == 0;

        _firstStep = SheetNames.Count > 1 ? ImportStep.Sheet : ImportStep.Preview;
        Step = _firstStep;
    }

    public ObservableCollection<string> SheetNames { get; } = new();
    public ObservableCollection<Preset> ExistingPresets { get; } = new();
    public IReadOnlyList<CultureInfo> AvailableCultures { get; }
    public ObservableCollection<ImportColumnViewModel> Columns { get; } = new();
    public ObservableCollection<ImportPreviewRow> PreviewRows { get; } = new();

    public IReadOnlyList<string> ColumnHeaders { get; private set; } = [];

    [ObservableProperty]
    public partial string? SelectedSheetName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsResult))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(IsSheetStep))]
    [NotifyPropertyChangedFor(nameof(IsPreviewStep))]
    [NotifyPropertyChangedFor(nameof(IsTargetStep))]
    [NotifyPropertyChangedFor(nameof(IsMapStep))]
    [NotifyPropertyChangedFor(nameof(StepTitle))]
    public partial ImportStep Step { get; set; }

    [ObservableProperty]
    public partial bool FirstRowIsHeader { get; set; } = true;

    [ObservableProperty]
    public partial bool Transpose { get; set; }

    [ObservableProperty]
    public partial CultureInfo SourceCulture { get; set; } = CultureInfo.InvariantCulture;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExistingMode))]
    public partial bool CreateNewCollection { get; set; }

    [ObservableProperty]
    public partial Preset? SelectedPreset { get; set; }

    [ObservableProperty]
    public partial string NewCollectionName { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SummaryImportedText))]
    [NotifyPropertyChangedFor(nameof(SummarySkippedText))]
    [NotifyPropertyChangedFor(nameof(SummaryWarningsText))]
    [NotifyPropertyChangedFor(nameof(HasSkipped))]
    [NotifyPropertyChangedFor(nameof(HasWarnings))]
    public partial ImportSummary? Summary { get; set; }

    public bool IsResult => Step == ImportStep.Result;
    public bool CanGoNext => Step != ImportStep.Result;
    public bool IsSheetStep => Step == ImportStep.Sheet;
    public bool IsPreviewStep => Step == ImportStep.Preview;
    public bool IsTargetStep => Step == ImportStep.Target;
    public bool IsMapStep => Step == ImportStep.Map;
    public bool IsExistingMode => !CreateNewCollection;

    public string StepTitle => Step switch
    {
        ImportStep.Sheet => LocalizationService.Instance["Import_Step_Sheet"],
        ImportStep.Preview => LocalizationService.Instance["Import_Step_Preview"],
        ImportStep.Target => LocalizationService.Instance["Import_Step_Target"],
        ImportStep.Map => LocalizationService.Instance["Import_Step_Map"],
        _ => LocalizationService.Instance["Import_Step_Result"]
    };

    public string SummaryImportedText =>
        string.Format(LocalizationService.Instance["Import_Summary_Imported"], Summary?.Imported ?? 0);

    public bool HasSkipped => Summary is { Skipped.Count: > 0 };
    public bool HasWarnings => Summary is { Warnings.Count: > 0 };

    public string SummarySkippedText => Summary is null
        ? string.Empty
        : string.Format(LocalizationService.Instance["Import_Summary_Skipped"], Summary.Skipped.Count)
          + "\n" + string.Join("\n", Summary.Skipped.Select(s => $"#{s.RowNumber}: {DescribeIssue(s)}"));

    public string SummaryWarningsText => Summary is null
        ? string.Empty
        : string.Format(LocalizationService.Instance["Import_Summary_Warnings"], Summary.Warnings.Count)
          + "\n" + string.Join("\n", Summary.Warnings.Select(w => $"#{w.RowNumber}: {DescribeIssue(w)}"));

    private string DescribeIssue(ImportIssue issue) => issue.Kind switch
    {
        ImportIssueKind.NoValues => string.Format(LocalizationService.Instance["Import_Issue_NoValues"], issue.Detail),
        ImportIssueKind.UnparsedCells => string.Format(LocalizationService.Instance["Import_Issue_Unparsed"], issue.Detail),
        _ => issue.Detail
    };

    partial void OnSelectedSheetNameChanged(string? value) => Recompute();
    partial void OnFirstRowIsHeaderChanged(bool value) => Recompute();
    partial void OnTransposeChanged(bool value) => Recompute();

    private void Recompute()
    {
        if (SelectedSheetName is null) return;
        var sheet = _data.Sheets.FirstOrDefault(s => s.Name == SelectedSheetName);
        if (sheet is null) return;

        SourceCulture = _cultureDetector.Detect(sheet.Rows, AvailableCultures, SourceCulture);
        _shaped = _gridShaper.Shape(sheet.Rows, Transpose, FirstRowIsHeader);

        var headers = new List<string>(_shaped.Headers.Count);
        for (var i = 0; i < _shaped.Headers.Count; i++)
            headers.Add(ColumnDisplayName(_shaped.Headers[i], i));
        ColumnHeaders = headers;
        OnPropertyChanged(nameof(ColumnHeaders));

        PreviewRows.Clear();
        foreach (var row in _shaped.Rows.Take(100))
            PreviewRows.Add(new ImportPreviewRow(row.Select(c => c.Text ?? string.Empty).ToList()));

        Columns.Clear();
        for (var i = 0; i < headers.Count; i++)
            Columns.Add(new ImportColumnViewModel(i, headers[i], SampleColumn(i)));
    }

    private string ColumnDisplayName(string header, int index) =>
        string.IsNullOrWhiteSpace(header)
            ? string.Format(LocalizationService.Instance["Import_ColumnFallback"], index + 1)
            : header;

    private IReadOnlyList<string> SampleColumn(int index) =>
        _shaped.Rows
            .Select(r => index < r.Count ? r[index].Text : null)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Take(3)
            .Select(t => t!)
            .ToList();

    [RelayCommand]
    private void Cancel() => _onClose();

    [RelayCommand]
    private void Back()
    {
        if (Step == _firstStep)
        {
            _onClose();
            return;
        }
        if (Step == ImportStep.Result) return;
        Step -= 1;
    }

    [RelayCommand]
    private async Task Next()
    {
        switch (Step)
        {
            case ImportStep.Sheet:
                if (SelectedSheetName is null)
                {
                    await WarnAsync("Import_NoSheet");
                    return;
                }
                Step = ImportStep.Preview;
                break;
            case ImportStep.Preview:
                if (_shaped.Rows.Count == 0)
                {
                    await WarnAsync("Import_NoData");
                    return;
                }
                Step = ImportStep.Target;
                break;
            case ImportStep.Target:
                if (!await PrepareMappingAsync()) return;
                Step = ImportStep.Map;
                break;
            case ImportStep.Map:
                await RunImportAsync();
                break;
        }
    }

    private async Task<bool> PrepareMappingAsync()
    {
        if (CreateNewCollection)
        {
            if (string.IsNullOrWhiteSpace(NewCollectionName))
            {
                await WarnAsync("Import_NameRequired");
                return false;
            }
            BuildNewCollectionColumns();
            return true;
        }

        if (SelectedPreset is null)
        {
            await WarnAsync("Import_NoCollectionSelected");
            return false;
        }
        await BuildExistingCollectionColumnsAsync(SelectedPreset);
        return true;
    }

    private async Task BuildExistingCollectionColumnsAsync(Preset preset)
    {
        var effective = await _presetUseCase.GetEffectiveFieldsAsync(preset.Id);
        var skip = new ColumnTargetOption(LocalizationService.Instance["Import_Skip"], null, false, true, true);
        var title = new ColumnTargetOption(LocalizationService.Instance["Import_TitleColumn"], null, true, false, true);
        var fieldOptions = effective.Fields
            .Where(f => !f.IsTitleField)
            .Select(f => new ColumnTargetOption(f.Label, f, false, false, f is ITextImportable))
            .ToList();

        var claimed = new HashSet<Guid>();
        foreach (var column in Columns)
        {
            column.TargetOptions.Clear();
            column.TargetOptions.Add(skip);
            column.TargetOptions.Add(title);
            foreach (var option in fieldOptions)
                column.TargetOptions.Add(option);

            var match = fieldOptions.FirstOrDefault(o =>
                o.IsMappable && o.Field is not null && !claimed.Contains(o.Field.Id)
                && string.Equals(o.Label, column.Header, StringComparison.OrdinalIgnoreCase));
            if (match?.Field is not null) claimed.Add(match.Field.Id);
            column.SelectedTarget = match ?? skip;
        }
    }

    private void BuildNewCollectionColumns()
    {
        var titleAssigned = false;
        foreach (var column in Columns)
        {
            column.TypeChoices.Clear();
            foreach (var choice in _importableTypes)
                column.TypeChoices.Add(choice);

            var inferred = _inference.Infer(ColumnCells(column.ColumnIndex), SourceCulture);
            column.SelectedTypeChoice = _importableTypes.FirstOrDefault(c => c.Type == inferred.GetType())
                ?? _importableTypes.FirstOrDefault();

            if (!titleAssigned && column.IsSelected)
            {
                column.IsTitle = true;
                titleAssigned = true;
            }
        }
    }

    private IReadOnlyList<WorkbookCell> ColumnCells(int index) =>
        _shaped.Rows.Select(r => index < r.Count ? r[index] : new WorkbookCell(null, WorkbookCellKind.Blank)).ToList();

    private async Task RunImportAsync()
    {
        try
        {
            if (CreateNewCollection)
            {
                var columns = BuildNewFieldColumns();
                if (columns.Count == 0)
                {
                    await WarnAsync("Import_NoColumnsMapped");
                    return;
                }
                var (preset, summary) = await _importService.ImportNewAsync(NewCollectionName, _shaped, columns, SourceCulture);
                _importedPreset = preset;
                Summary = summary;
            }
            else
            {
                var mappings = BuildExistingMappings();
                if (mappings.Count == 0)
                {
                    await WarnAsync("Import_NoColumnsMapped");
                    return;
                }
                Summary = await _importService.ImportExistingAsync(SelectedPreset!.Id, _shaped, mappings, SourceCulture);
                _importedPreset = SelectedPreset;
            }
            Step = ImportStep.Result;
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Excel import failed");
            await _dialogService.ShowMessageAsync(ex.Message, LocalizationService.Instance["Import_Title"]);
        }
    }

    private List<ColumnMapping> BuildExistingMappings()
    {
        var mappings = new List<ColumnMapping>();
        foreach (var column in Columns)
        {
            if (!column.IsSelected || column.SelectedTarget is null || column.SelectedTarget.IsSkip) continue;
            var target = column.SelectedTarget;
            if (target.IsTitle)
                mappings.Add(new ColumnMapping(column.ColumnIndex, Guid.Empty, true));
            else if (target.Field is not null && target.IsMappable)
                mappings.Add(new ColumnMapping(column.ColumnIndex, target.Field.Id, false));
        }
        return mappings;
    }

    private List<NewFieldColumn> BuildNewFieldColumns()
    {
        var columns = new List<NewFieldColumn>();
        var titleAssigned = false;
        foreach (var column in Columns.Where(c => c.IsSelected))
        {
            if (column.IsTitle && !titleAssigned)
            {
                titleAssigned = true;
                columns.Add(new NewFieldColumn(column.ColumnIndex, new DisplayNameFieldDefinition(), true));
                continue;
            }
            var choice = column.SelectedTypeChoice ?? _importableTypes.FirstOrDefault();
            if (choice is null) continue;
            var definition = (FieldDefinition)Activator.CreateInstance(choice.Type)!;
            definition.Label = string.IsNullOrWhiteSpace(column.Label) ? column.Header : column.Label;
            columns.Add(new NewFieldColumn(column.ColumnIndex, definition, false));
        }
        return columns;
    }

    [RelayCommand]
    private async Task Finish()
    {
        if (_importedPreset is not null && _onFinished is not null)
            await _onFinished(_importedPreset);
        else
            _onClose();
    }

    private Task WarnAsync(string key) =>
        _dialogService.ShowMessageAsync(LocalizationService.Instance[key], LocalizationService.Instance["Import_Title"]);

    private IReadOnlyList<FieldTypeChoice> BuildImportableTypeChoices() =>
        typeof(FieldDefinition).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsGenericTypeDefinition && typeof(FieldDefinition).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<FieldCatalogAttribute>() is not null)
            .Where(t => Activator.CreateInstance(t) is ITextImportable)
            .Select(t => new FieldTypeChoice(t, t.ToLocalizedString()))
            .OrderBy(c => c.Name, StringComparer.CurrentCulture)
            .ToList();

    private IReadOnlyList<CultureInfo> BuildAvailableCultures()
    {
        var cultures = new List<CultureInfo>
        {
            CultureInfo.CurrentCulture,
            new("en-US"),
            new("de-DE"),
            new("fr-FR"),
            CultureInfo.InvariantCulture
        };
        return cultures
            .GroupBy(c => c.Name)
            .Select(g => g.First())
            .ToList();
    }
}
