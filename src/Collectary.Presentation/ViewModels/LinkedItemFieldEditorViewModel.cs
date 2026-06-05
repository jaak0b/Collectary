using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

/// <summary>A selectable target for a link field: the item's <see cref="Id"/> and a human-readable <see cref="Display"/>.</summary>
public sealed record LinkedItemOption(Guid Id, string Display);

public partial class LinkedItemFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly LinkedItemFieldDefinition _definition;
    private readonly LinkedItemFieldValue _value;
    private readonly ItemEditingContext _context;

    public ObservableCollection<LinkedItemOption> Candidates { get; } = new();

    [ObservableProperty]
    public partial LinkedItemOption? SelectedItem { get; set; }

    public LinkedItemFieldEditorViewModel(
        LinkedItemFieldDefinition definition,
        LinkedItemFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;

        // Show the existing link straight away, before the full candidate list is loaded.
        if (value.TargetItemId is { } id)
        {
            var current = new LinkedItemOption(id, value.TargetDisplay ?? "");
            Candidates.Add(current);
            SelectedItem = current;
        }
    }

    public override FieldDefinition Definition => _definition;

    [RelayCommand]
    private async Task LoadCandidatesAsync()
    {
        var loaded = await _context.LoadLinkableItemsAsync();
        var selectedId = SelectedItem?.Id;
        Candidates.Clear();
        foreach (var option in loaded) Candidates.Add(option);
        if (selectedId is { } id)
            SelectedItem = Candidates.FirstOrDefault(c => c.Id == id) ?? SelectedItem;
    }

    public override FieldValue GetCurrentValue()
    {
        _value.TargetItemId = SelectedItem?.Id;
        _value.TargetDisplay = SelectedItem?.Display;
        return _value;
    }
}
