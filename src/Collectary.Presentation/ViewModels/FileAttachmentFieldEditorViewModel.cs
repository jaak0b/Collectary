using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

/// <summary>One attached document within a <see cref="FileAttachmentFieldEditorViewModel"/>.</summary>
public class FileAttachmentEntryViewModel : ViewModelBase
{
    public string Key { get; }
    public string FileName { get; }

    public FileAttachmentEntryViewModel(string key, string fileName)
    {
        Key = key;
        FileName = fileName;
    }
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
            Attachments.Add(new FileAttachmentEntryViewModel(file.Key, file.FileName));
        Attachments.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasAttachments));
    }

    public override FieldDefinition Definition => _definition;

    [RelayCommand]
    private async Task AddFileAsync()
    {
        var result = await _context.PickAndStoreFileAsync();
        if (result is null) return;
        Attachments.Add(new FileAttachmentEntryViewModel(result.Value.Key, result.Value.FileName));
    }

    [RelayCommand]
    private async Task OpenFileAsync(FileAttachmentEntryViewModel entry)
    {
        if (entry is null) return;
        await _context.ExportFileAsync(entry.Key, entry.FileName);
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
