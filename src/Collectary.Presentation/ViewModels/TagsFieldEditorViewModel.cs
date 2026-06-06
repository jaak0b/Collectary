using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class TagsFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly TagsFieldDefinition _definition;
    private readonly TagsFieldValue _value;

    public ObservableCollection<string> Tags { get; } = new();

    [ObservableProperty]
    public partial string? NewTag { get; set; }

    public TagsFieldEditorViewModel(TagsFieldDefinition definition, TagsFieldValue value)
    {
        _definition = definition;
        _value = value;
        foreach (var tag in value.Tags)
            Tags.Add(tag);
    }

    public override FieldDefinition Definition => _definition;

    [RelayCommand]
    private void AddTag()
    {
        if (string.IsNullOrWhiteSpace(NewTag)) return;
        var tag = NewTag.Trim();
        if (!Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            Tags.Add(tag);
        NewTag = null;
    }

    [RelayCommand]
    private void RemoveTag(string tag) => Tags.Remove(tag);

    public override void Randomize(Services.ISampleData data)
    {
        Tags.Clear();
        foreach (var tag in data.WordList(3))
            Tags.Add(tag);
    }

    public override FieldValue GetCurrentValue()
    {
        _value.Tags = Tags.ToList();
        return _value;
    }
}
