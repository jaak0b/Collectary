using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

/// <summary>One picture within a <see cref="MultiImageFieldEditorViewModel"/> gallery.</summary>
public partial class MultiImageEntryViewModel : ViewModelBase
{
    private readonly ItemEditingContext _context;
    private readonly Func<MultiImageEntryViewModel, Task> _remove;

    public string Key { get; }
    public string FileName { get; }

    [ObservableProperty]
    public partial Bitmap? Bitmap { get; set; }

    public MultiImageEntryViewModel(
        string key,
        string fileName,
        Bitmap? bitmap,
        ItemEditingContext context,
        Func<MultiImageEntryViewModel, Task> remove)
    {
        Key = key;
        FileName = fileName;
        Bitmap = bitmap;
        _context = context;
        _remove = remove;
    }

    [RelayCommand]
    private Task SaveAs() => _context.ExportImageAsync(Key, FileName);

    [RelayCommand]
    private Task Delete() => _remove(this);
}

public partial class MultiImageFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly MultiImageFieldDefinition _definition;
    private readonly MultiImageFieldValue _value;
    private readonly ItemEditingContext _context;

    public ObservableCollection<MultiImageEntryViewModel> Images { get; } = new();

    public bool HasImages => Images.Count > 0;

    public MultiImageFieldEditorViewModel(
        MultiImageFieldDefinition definition,
        MultiImageFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;

        foreach (var picture in value.Pictures)
            Images.Add(CreateEntry(picture.Key, picture.FileName, _context.LoadImageBitmap(picture.Key)));
        Images.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasImages));
    }

    public override FieldDefinition Definition => _definition;

    private MultiImageEntryViewModel CreateEntry(string key, string fileName, Bitmap? bitmap) =>
        new(key, fileName, bitmap, _context, RemoveImageAsync);

    [RelayCommand]
    private async Task AddImageAsync()
    {
        var result = await _context.PickAndStoreImageAsync();
        if (result is null) return;
        Images.Add(CreateEntry(result.Value.Key, result.Value.FileName, result.Value.Preview));
    }

    [RelayCommand]
    private async Task RemoveImageAsync(MultiImageEntryViewModel entry)
    {
        if (!Images.Remove(entry)) return;
        await _context.DeleteImageAsync(entry.Key);
    }

    [RelayCommand]
    private void MoveUp(MultiImageEntryViewModel entry)
    {
        var i = Images.IndexOf(entry);
        if (i > 0) Images.Move(i, i - 1);
    }

    public override FieldValue GetCurrentValue()
    {
        _value.Pictures = Images.Select(e => new MultiImagePicture(e.Key, e.FileName)).ToList();
        return _value;
    }
}
