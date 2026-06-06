using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Collectary.Core.Domain;

namespace Collectary.Presentation.ViewModels.Import;

public sealed class ColumnTargetOption
{
    public ColumnTargetOption(string label, FieldDefinition? field, bool isTitle, bool isSkip, bool isMappable)
    {
        Label = label;
        Field = field;
        IsTitle = isTitle;
        IsSkip = isSkip;
        IsMappable = isMappable;
    }

    public string Label { get; }
    public FieldDefinition? Field { get; }
    public bool IsTitle { get; }
    public bool IsSkip { get; }
    public bool IsMappable { get; }
}

public sealed class FieldTypeChoice
{
    public FieldTypeChoice(Type type, string name)
    {
        Type = type;
        Name = name;
    }

    public Type Type { get; }
    public string Name { get; }
}

public sealed class ImportPreviewRow
{
    public ImportPreviewRow(IReadOnlyList<string> cells) => Cells = cells;

    public IReadOnlyList<string> Cells { get; }

    public string this[int index] => index >= 0 && index < Cells.Count ? Cells[index] : string.Empty;
}

public partial class ImportColumnViewModel : ObservableObject
{
    public ImportColumnViewModel(int columnIndex, string header, IReadOnlyList<string> samples)
    {
        ColumnIndex = columnIndex;
        Header = header;
        Samples = samples;
        Label = header;
    }

    public int ColumnIndex { get; }
    public string Header { get; }
    public IReadOnlyList<string> Samples { get; }
    public string SamplePreview => string.Join(", ", Samples);

    [ObservableProperty]
    public partial bool IsSelected { get; set; } = true;

    public ObservableCollection<ColumnTargetOption> TargetOptions { get; } = new();

    [ObservableProperty]
    public partial ColumnTargetOption? SelectedTarget { get; set; }

    public ObservableCollection<FieldTypeChoice> TypeChoices { get; } = new();

    [ObservableProperty]
    public partial FieldTypeChoice? SelectedTypeChoice { get; set; }

    [ObservableProperty]
    public partial string Label { get; set; }

    [ObservableProperty]
    public partial bool IsTitle { get; set; }
}
