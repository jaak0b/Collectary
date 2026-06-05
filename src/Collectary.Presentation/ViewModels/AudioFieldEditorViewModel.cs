using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;

namespace Collectary.Presentation.ViewModels;

public partial class AudioFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly AudioFieldDefinition _definition;
    private readonly AudioFieldValue _value;
    private readonly ItemEditingContext _context;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAudio))]
    public partial string? AudioKey { get; set; }

    [ObservableProperty]
    public partial int? DurationSeconds { get; set; }

    public bool HasAudio => !string.IsNullOrEmpty(AudioKey);

    public AudioFieldEditorViewModel(
        AudioFieldDefinition definition,
        AudioFieldValue value,
        ItemEditingContext context)
    {
        _definition = definition;
        _value = value;
        _context = context;
        AudioKey = value.AudioKey;
        DurationSeconds = value.DurationSeconds;
    }

    public override FieldDefinition Definition => _definition;

    [RelayCommand]
    private async Task RecordAsync()
    {
        var result = await _context.RecordAudioAsync();
        if (result is null) return;
        AudioKey = result.Value.Key;
        DurationSeconds = result.Value.DurationSeconds;
    }

    [RelayCommand]
    private async Task PlayAsync()
    {
        if (!HasAudio) return;
        await _context.PlayAudioAsync(AudioKey!);
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!HasAudio) return;
        await _context.DeleteFileAsync(AudioKey!);
        AudioKey = null;
        DurationSeconds = null;
    }

    public override FieldValue GetCurrentValue()
    {
        _value.AudioKey = AudioKey;
        _value.DurationSeconds = DurationSeconds;
        return _value;
    }
}
