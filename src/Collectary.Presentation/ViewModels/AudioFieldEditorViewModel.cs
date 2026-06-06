using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;

namespace Collectary.Presentation.ViewModels;

public partial class AudioFieldEditorViewModel : FieldEditorViewModelBase
{
    private readonly AudioFieldDefinition _definition;
    private readonly AudioFieldValue _value;
    private readonly ItemEditingContext _context;

    public ObservableCollection<AudioInputDevice> Microphones { get; } = new();

    [ObservableProperty]
    public partial AudioInputDevice? SelectedMicrophone { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAudio))]
    public partial string? AudioKey { get; set; }

    [ObservableProperty]
    public partial int? DurationSeconds { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecordButtonLabel))]
    public partial bool IsRecording { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayButtonLabel))]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PlayButtonLabel))]
    public partial bool IsPaused { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool HasAudio => !string.IsNullOrEmpty(AudioKey);
    public bool AudioAvailable => _context.AudioRecorder is not null;

    public string RecordButtonLabel =>
        LocalizationService.Instance[IsRecording ? "Audio_StopRecording" : "Audio_StartRecording"];

    public string PlayButtonLabel =>
        LocalizationService.Instance[IsPlaying && !IsPaused ? "Audio_Pause" : "Audio_Play"];

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

        if (_context.AudioRecorder is { } recorder)
        {
            foreach (var device in recorder.GetInputDevices())
                Microphones.Add(device);
            SelectedMicrophone = Microphones.FirstOrDefault();
        }
    }

    public override FieldDefinition Definition => _definition;

    [RelayCommand]
    private async Task ToggleRecordAsync()
    {
        if (_context.AudioRecorder is not { } recorder) return;
        try
        {
            ErrorMessage = null;
            if (!IsRecording)
            {
                recorder.Start(SelectedMicrophone?.Id);
                IsRecording = true;
                return;
            }

            var result = await recorder.StopAsync();
            IsRecording = false;
            if (result is null) return;

            AudioKey = await _context.StoreAudioAsync(result.Data);
            DurationSeconds = result.DurationSeconds;
        }
        catch (Exception ex)
        {
            IsRecording = false;
            AppLogger.Log.Error(ex, "Audio recording failed");
            ErrorMessage = LocalizationService.Instance["Audio_RecordFailed"];
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task TogglePlaybackAsync()
    {
        if (_context.AudioPlayer is not { } player || !HasAudio) return;

        if (IsPlaying && !IsPaused)
        {
            player.Pause();
            IsPaused = true;
            return;
        }

        if (IsPaused)
        {
            player.Resume();
            IsPaused = false;
            return;
        }

        var stream = _context.OpenAudioStream(AudioKey!);
        if (stream is null) return;

        try
        {
            ErrorMessage = null;
            IsPlaying = true;
            await player.PlayAsync(stream);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Audio playback failed");
            ErrorMessage = LocalizationService.Instance["Audio_RecordFailed"];
        }
        finally
        {
            IsPlaying = false;
            IsPaused = false;
        }
    }

    public override FieldValue GetCurrentValue()
    {
        _value.AudioKey = AudioKey;
        _value.DurationSeconds = DurationSeconds;
        return _value;
    }
}
