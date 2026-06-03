using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Ports;
using Collectary.UI.Localization;
using Collectary.UI.Services;
using Serilog;

namespace Collectary.UI.ViewModels;

public partial class HomeViewModel : ViewModelBase
{
    private readonly IPresetUseCase _presetUseCase;
    private readonly IItemUseCase _itemUseCase;
    private readonly IDialogService _dialogService;

    public Action<Core.Domain.Preset>? OnNavigateToPreset { get; set; }
    public Action? OnCreatePreset { get; set; }
    public Action<Core.Domain.Preset>? OnEditPreset { get; set; }
    public Func<Core.Domain.Preset, Task>? OnDeletePreset { get; set; }
    public Action? OnNavigateToSystemFields { get; set; }

    public ObservableCollection<PresetRowViewModel> Rows { get; } = new();

    public HomeViewModel(IPresetUseCase presetUseCase, IItemUseCase itemUseCase, IDialogService dialogService)
    {
        _presetUseCase = presetUseCase;
        _itemUseCase = itemUseCase;
        _dialogService = dialogService;
    }

    public async Task LoadAsync()
    {
        try
        {
            Rows.Clear();
            var presets = await _presetUseCase.GetAllPresetsAsync();
            foreach (var preset in presets)
            {
                var items = await _itemUseCase.GetItemsForPresetAsync(preset.Id);
                var captured = preset;
                Rows.Add(new PresetRowViewModel(
                    preset: captured,
                    itemCount: items.Count,
                    onNavigate: () => OnNavigateToPreset?.Invoke(captured),
                    onEdit: () => OnEditPreset?.Invoke(captured),
                    onDelete: async () =>
                    {
                        if (!await _dialogService.ConfirmDeleteAsync(captured.Name)) return;
                        if (OnDeletePreset is not null) await OnDeletePreset(captured);
                    }));
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Failed to load presets");
            await _dialogService.ShowMessageAsync(LocalizationService.Instance["CouldNotLoad"], LocalizationService.Instance["CouldNotLoad"]);
        }
    }

    public async Task SavePresetOrderAsync()
    {
        var ordered = Rows.Select(r => r.Preset).ToList();
        await _presetUseCase.UpdatePresetOrderAsync(ordered);
    }

    [RelayCommand]
    private void CreatePreset() => OnCreatePreset?.Invoke();

    [RelayCommand]
    private void NavigateToSystemFields() => OnNavigateToSystemFields?.Invoke();
}
