using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

/// <summary>One attached document within a <see cref="FileAttachmentFieldEditorViewModel"/>. The user edits the base name freely; the extension is fixed.</summary>
public partial class FileAttachmentEntryViewModel : ViewModelBase
{
    private readonly ItemEditingContext _context;
    private readonly Func<FileAttachmentEntryViewModel, Task> _remove;
    private readonly string _originalFileName;

    public string Key { get; }
    public string Extension { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileName))]
    public partial string EditingName { get; set; }

    public string FileName =>
        string.IsNullOrWhiteSpace(EditingName) ? _originalFileName : EditingName.Trim() + Extension;

    public FileAttachmentEntryViewModel(
        string key,
        string fileName,
        ItemEditingContext context,
        Func<FileAttachmentEntryViewModel, Task> remove)
    {
        Key = key;
        _originalFileName = fileName;
        Extension = Path.GetExtension(fileName);
        EditingName = Path.GetFileNameWithoutExtension(fileName);
        _context = context;
        _remove = remove;
    }

    [RelayCommand]
    private Task SaveAs() => _context.ExportFileAsync(Key, FileName);

    [RelayCommand]
    private Task Delete() => _remove(this);
}

public partial class FileAttachmentFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly FileAttachmentFieldDefinition _definition;
    private readonly FileAttachmentFieldValue _value;
    private readonly ItemEditingContext _context;

    public ObservableCollection<FileAttachmentEntryViewModel> Attachments { get; } = new();

    public bool HasAttachments => Attachments.Count > 0;

    public FileAttachmentFieldEditorViewModel(
        FileAttachmentFieldDefinition definition,
        FileAttachmentFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;

        foreach (var file in value.Files)
            Attachments.Add(CreateEntry(file.Key, file.FileName));
        Attachments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAttachments));
    }

    public override FieldDefinition Definition => _definition;

    private FileAttachmentEntryViewModel CreateEntry(string key, string fileName) =>
        new(key, fileName, _context, RemoveFileAsync);

    [RelayCommand]
    private async Task AddFileAsync()
    {
        var result = await _context.PickAndStoreFileAsync();
        if (result is null) return;
        Attachments.Add(CreateEntry(result.Value.Key, result.Value.FileName));
    }

    [RelayCommand]
    private async Task RemoveFileAsync(FileAttachmentEntryViewModel entry)
    {
        if (!Attachments.Remove(entry)) return;
        await _context.DeleteFileAsync(entry.Key);
    }

    public override FieldValue GetCurrentValue()
    {
        _value.Files = Attachments.Select(a => new FileAttachment(a.Key, a.FileName)).ToList();
        return _value;
    }
}
