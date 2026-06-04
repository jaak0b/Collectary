using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.Localization;

namespace Collectary.Presentation.ViewModels;

public partial class ShareDialogViewModel : ViewModelBase
{
    private readonly IShareUseCase _shares;
    private readonly Guid _presetId;
    private readonly Action? _onTransferred;

    public string CollectionName { get; }

    public ObservableCollection<ShareInfo> Shares { get; } = new();

    public IReadOnlyList<SharePermission> Permissions { get; } =
        new[] { SharePermission.Read, SharePermission.Edit };

    [ObservableProperty]
    public partial string TargetUsername { get; set; } = string.Empty;

    [ObservableProperty]
    public partial SharePermission SelectedPermission { get; set; } = SharePermission.Read;

    [ObservableProperty]
    public partial string TransferUsername { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string? StatusMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public bool HasShares => Shares.Count > 0;

    partial void OnErrorMessageChanged(string? value) => OnPropertyChanged(nameof(HasError));

    partial void OnStatusMessageChanged(string? value) => OnPropertyChanged(nameof(HasStatus));

    public ShareDialogViewModel(IShareUseCase shares, Guid presetId, string collectionName, Action? onTransferred = null)
    {
        _shares = shares;
        _presetId = presetId;
        _onTransferred = onTransferred;
        CollectionName = collectionName;
        Shares.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasShares));
    }

    public async Task LoadAsync()
    {
        try
        {
            Shares.Clear();
            foreach (var share in await _shares.ListSharesAsync(_presetId))
                Shares.Add(share);
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = LocalizationService.Instance["Share_NotOwner"];
        }
    }

    [RelayCommand]
    private async Task Share()
    {
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            await _shares.ShareAsync(_presetId, TargetUsername.Trim(), SelectedPermission);
            TargetUsername = string.Empty;
            await LoadAsync();
        }
        catch (UserNotFoundException)
        {
            ErrorMessage = LocalizationService.Instance["Share_UserNotFound"];
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = LocalizationService.Instance["Share_NotOwner"];
        }
        catch (InvalidOperationException)
        {
            ErrorMessage = LocalizationService.Instance["Share_Invalid"];
        }
    }

    [RelayCommand]
    private async Task Revoke(ShareInfo share)
    {
        ErrorMessage = null;
        await _shares.RevokeAsync(_presetId, share.Username);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Transfer()
    {
        ErrorMessage = null;
        StatusMessage = null;
        try
        {
            await _shares.TransferAsync(_presetId, TransferUsername.Trim());
            StatusMessage = LocalizationService.Instance["Share_TransferDone"];
            _onTransferred?.Invoke();
        }
        catch (UserNotFoundException)
        {
            ErrorMessage = LocalizationService.Instance["Share_UserNotFound"];
        }
        catch (UnauthorizedAccessException)
        {
            ErrorMessage = LocalizationService.Instance["Share_NotOwner"];
        }
        catch (InvalidOperationException)
        {
            ErrorMessage = LocalizationService.Instance["Share_Invalid"];
        }
    }
}
