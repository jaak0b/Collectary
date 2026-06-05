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
    public string Key { get; }

    [ObservableProperty]
    public partial Bitmap? Bitmap { get; set; }

    public MultiImageEntryViewModel(string key, Bitmap? bitmap)
    {
        Key = key;
        Bitmap = bitmap;
    }
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

        foreach (var key in value.ImageKeys)
            Images.Add(new MultiImageEntryViewModel(key, _context.LoadImageBitmap(key)));
        Images.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasImages));
    }

    public override FieldDefinition Definition => _definition;

    [RelayCommand]
    private async Task AddImageAsync()
    {
        var result = await _context.PickAndStoreImageAsync();
        if (result is null) return;
        Images.Add(new MultiImageEntryViewModel(result.Value.Key, result.Value.Preview));
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
        _value.ImageKeys = Images.Select(e => e.Key).ToList();
        return _value;
    }
}
