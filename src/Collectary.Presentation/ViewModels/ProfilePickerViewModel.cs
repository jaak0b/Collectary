using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class ProfilePickerViewModel : ViewModelBase
{
    private readonly IProfileService _profiles;
    private readonly Func<User, Task> _onSelected;

    public ObservableCollection<ProfileTileViewModel> Profiles { get; } = new();

    [ObservableProperty]
    public partial bool IsAdding { get; set; }

    [ObservableProperty]
    public partial string NewProfileName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public ProfilePickerViewModel(IProfileService profiles, Func<User, Task> onSelected)
    {
        _profiles = profiles;
        _onSelected = onSelected;
    }

    public async Task LoadAsync()
    {
        Profiles.Clear();
        foreach (var profile in await _profiles.GetProfilesAsync())
            Profiles.Add(new ProfileTileViewModel(profile));
    }

    [RelayCommand]
    private async Task SelectProfile(ProfileTileViewModel tile) => await _onSelected(tile.Profile);

    [RelayCommand]
    private void BeginAdd()
    {
        IsAdding = true;
        NewProfileName = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAdding = false;
        NewProfileName = string.Empty;
        ErrorMessage = null;
    }

    [RelayCommand]
    private async Task CreateProfile()
    {
        if (string.IsNullOrWhiteSpace(NewProfileName))
        {
            ErrorMessage = LocalizationService.Instance["Profile_NameRequired"];
            return;
        }

        var profile = await _profiles.CreateProfileAsync(NewProfileName.Trim());
        await _onSelected(profile);
    }
}
