using System.Collections.ObjectModel;
using Autofac;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels.SystemFields;

namespace Collectary.Presentation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILifetimeScope _scope;
    private readonly IPresetUseCase _presetUseCase;
    private readonly IItemUseCase _itemUseCase;
    private readonly ISystemFieldUseCase _systemFieldUseCase;
    private readonly IListCellBuilder _listCellBuilder;
    private readonly IFieldEditorRegistry _editorRegistry;
    private readonly IImageStore _imageStore;
    private readonly IDialogService _dialogService;
    private readonly ISyncScheduler _syncScheduler;

    public Visual? Host { get; set; }

    [ObservableProperty]
    public partial ViewModelBase? ContentViewModel { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMobileSidebarVisible))]
    [NotifyPropertyChangedFor(nameof(IsDesktopSidebarVisible))]
    public partial bool IsSidebarOpen { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMobileSidebarVisible))]
    [NotifyPropertyChangedFor(nameof(IsDesktopSidebarVisible))]
    public partial bool IsNarrow { get; set; }

    public bool IsMobileSidebarVisible => IsNarrow && IsSidebarOpen;
    public bool IsDesktopSidebarVisible => !IsNarrow && IsSidebarOpen;

    [ObservableProperty]
    public partial double SidebarWidth { get; set; } = 260;

    public HomeViewModel? SidebarViewModel { get; private set; }

    public SyncViewModel Sync { get; }

    [RelayCommand]
    private async Task ResolveConflicts() => await _dialogService.ShowSyncConflictsAsync(Sync);

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
        AppPreferences.Update(p => p with { SidebarOpen = IsSidebarOpen });
    }

    [RelayCommand]
    private async Task NavigateHome() => await NavigateToHomeAsync();

    [RelayCommand]
    private void NavigateToSettings()
    {
        var vm = new SettingsViewModel(
            navigateToSystemFields: NavigateToSystemFieldLibrary,
            pickFolder: PickSyncFolderAsync,
            onSyncChanged: OnSyncSettingsChanged,
            connectCloud: ConnectCloudAsync,
            pickCloudFolder: SetUpCloudFolderAsync,
            disconnectCloud: DisconnectCloudAsync,
            detectInstalledCloudFolder: () => new InstalledCloudFolderDetector().Detect());
        ResetBreadcrumb(LocalizationService.Instance["Settings"], vm);
    }

    private async Task<string?> ConnectCloudAsync(CloudProvider provider)
    {
        var auth = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudAuthClient>(_scope, provider);
        if (auth is null) return null;
        try
        {
            await auth.SignInInteractiveAsync(CancellationToken.None);
            return auth.Account;
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Cloud sign-in failed for {Provider}", provider);
            await ShowCloudErrorAsync(ex);
            return null;
        }
    }

    private async Task<CloudFolder?> SetUpCloudFolderAsync(CloudProvider provider)
    {
        var rootProvider = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudRootProvider>(_scope, provider);
        var store = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudFileStore>(_scope, provider);
        if (rootProvider is null || store is null) return null;
        try
        {
            var root = await rootProvider.GetRootFolderAsync(CancellationToken.None);
            var picker = new CloudFolderPickerViewModel(store, root);
            return await _dialogService.ShowCloudFolderPickerAsync(picker);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Cloud folder selection failed for {Provider}", provider);
            await ShowCloudErrorAsync(ex);
            return null;
        }
    }

    private Task ShowCloudErrorAsync(Exception ex) =>
        _dialogService.ShowMessageAsync(
            $"{LocalizationService.Instance["Cloud_AuthFailed"]}\n\n{ex.Message}",
            LocalizationService.Instance["Settings"]);

    private async Task DisconnectCloudAsync(CloudProvider provider)
    {
        var auth = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudAuthClient>(_scope, provider);
        if (auth is not null) await auth.SignOutAsync();
    }

    private void OnSyncSettingsChanged()
    {
        Sync.Refresh();
        ConfigureAutoSyncTimer(AppPreferences.Load());
        if (Sync.IsConfigured) _ = SyncThenReloadAsync();
    }

    public ObservableCollection<BreadcrumbNode> Breadcrumbs { get; } = new();

    public bool HasBreadcrumbs => Breadcrumbs.Count > 0;

    private void PushBreadcrumb(string title, ViewModelBase content)
    {
        Breadcrumbs.Add(new BreadcrumbNode(title, content));
        OnPropertyChanged(nameof(HasBreadcrumbs));
        ContentViewModel = content;
    }

    private void ResetBreadcrumb(string title, ViewModelBase content)
    {
        Breadcrumbs.Clear();
        PushBreadcrumb(title, content);
    }

    private void GoBack()
    {
        if (Breadcrumbs.Count <= 1) return;
        Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        ContentViewModel = Breadcrumbs[^1].Content;
    }

    [RelayCommand]
    private void NavigateToBreadcrumb(BreadcrumbNode node)
    {
        var index = Breadcrumbs.IndexOf(node);
        if (index < 0) return;
        while (Breadcrumbs.Count > index + 1)
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        ContentViewModel = node.Content;
    }

    public MainWindowViewModel(ILifetimeScope scope, IPresetUseCase presetUseCase, IItemUseCase itemUseCase, ISystemFieldUseCase systemFieldUseCase, IListCellBuilder listCellBuilder, IFieldEditorRegistry editorRegistry, IImageStore imageStore, IDialogService dialogService, ISyncScheduler syncScheduler)
    {
        _scope = scope;
        _presetUseCase = presetUseCase;
        _itemUseCase = itemUseCase;
        _systemFieldUseCase = systemFieldUseCase;
        _listCellBuilder = listCellBuilder;
        _editorRegistry = editorRegistry;
        _imageStore = imageStore;
        _dialogService = dialogService;
        _syncScheduler = syncScheduler;
        Sync = new SyncViewModel(scope.Resolve<ISyncService>(), scope.Resolve<ISyncStatus>());
        Sync.Synced += OnSynced;
        Breadcrumbs.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasBreadcrumbs));
    }

    private async Task<string?> PickSyncFolderAsync()
    {
        var storage = TopLevel.GetTopLevel(Host)?.StorageProvider;
        if (storage is null) return null;
        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task InitializeAsync()
    {
        var prefs = AppPreferences.Load();
        SidebarWidth = prefs.SidebarWidth > 0 ? prefs.SidebarWidth : 260;
        IsSidebarOpen = prefs.SidebarOpen;

        var home = new HomeViewModel(_presetUseCase, _itemUseCase, _dialogService);
        home.OnNavigateToPreset = NavigateToPreset;
        home.OnCreatePreset = () => NavigateToPresetEditor(null);
        home.OnCreateFromTemplate = NavigateToTemplatePicker;
        home.OnEditPreset = preset => NavigateToPresetEditor(preset);
        home.OnNavigateToSystemFields = NavigateToSystemFieldLibrary;
        home.OnSharePreset = SharePreset;
        home.OnDeletePreset = async (preset) =>
        {
            await _presetUseCase.DeletePresetAsync(preset.Id);
            SidebarViewModel?.ClearSelection();
            await SidebarViewModel!.LoadAsync();
            ContentViewModel = new WelcomeViewModel();
            Breadcrumbs.Clear();
        };
        SidebarViewModel = home;
        OnPropertyChanged(nameof(SidebarViewModel));

        ContentViewModel = new WelcomeViewModel();
        await home.LoadAsync();

        StartSync(prefs);
    }

    private void StartSync(AppPreferencesData prefs)
    {
        ConfigureAutoSyncTimer(prefs);
        if (Sync.IsConfigured) _ = SyncThenReloadAsync();
    }

    private void ConfigureAutoSyncTimer(AppPreferencesData prefs)
    {
        _syncScheduler.Stop();

        if (!Sync.IsConfigured || !prefs.AutoSyncEnabled) return;

        _syncScheduler.Start(
            TimeSpan.FromMinutes(Math.Max(1, prefs.AutoSyncIntervalMinutes)),
            SyncThenReloadAsync);
    }

    private async Task SyncThenReloadAsync() => await Sync.SyncNowCommand.ExecuteAsync(null);

    private async void OnSynced()
    {
        try
        {
            if (SidebarViewModel is not null) await SidebarViewModel.LoadAsync();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Reload after sync failed");
        }
    }

    public void Dispose()
    {
        _syncScheduler.Dispose();
        Sync.Synced -= OnSynced;
    }

    private async Task NavigateToHomeAsync()
    {
        AppLogger.Log.Debug("Navigate: Home");
        Breadcrumbs.Clear();
        ContentViewModel = new WelcomeViewModel();
        SidebarViewModel?.ClearSelection();
        IsSidebarOpen = true;
        AppPreferences.Update(p => p with { SidebarOpen = true });
        if (SidebarViewModel is not null)
            await SidebarViewModel.LoadAsync();
    }

    private void SharePreset(Preset preset)
    {
        AppLogger.Log.Debug("Share: preset id={Id} name={Name}", preset.Id, preset.Name);
        var shareUseCase = _scope.Resolve<IShareUseCase>();
        var vm = new ShareDialogViewModel(
            shareUseCase,
            preset.Id,
            preset.Name,
            onTransferred: () => { _ = SidebarViewModel?.LoadAsync(); });
        _ = ShowShareDialogAsync(vm);
    }

    private async Task ShowShareDialogAsync(ShareDialogViewModel vm)
    {
        await vm.LoadAsync();
        await _dialogService.ShowShareDialogAsync(vm);
        if (SidebarViewModel is not null)
            await SidebarViewModel.LoadAsync();
    }

    private void NavigateToPreset(Preset preset)
    {
        AppLogger.Log.Debug("Navigate: PresetDetail id={Id} name={Name}", preset.Id, preset.Name);
        var itemUseCase = _scope.Resolve<IItemUseCase>();
        var presetUseCase = _scope.Resolve<IPresetUseCase>();

        var vm = new PresetDetailViewModel(
            preset,
            itemUseCase,
            presetUseCase,
            _listCellBuilder,
            _dialogService,
            navigateToItemEditor: NavigateToItemEditor,
            navigateBack: () => { _ = NavigateToHomeAsync(); });

        ResetBreadcrumb(preset.Name, vm);
        _ = vm.LoadAsync();

        if (IsNarrow)
        {
            IsSidebarOpen = false;
            AppPreferences.Update(p => p with { SidebarOpen = false });
        }
    }

    private void NavigateToPresetEditor(Preset? existing, Preset? seed = null)
    {
        AppLogger.Log.Debug("Navigate: PresetEditor existing={Id} seed={Seed}", existing?.Id, seed?.Name);
        var presetUseCase = _scope.Resolve<IPresetUseCase>();
        var systemFieldUseCase = _scope.Resolve<ISystemFieldUseCase>();
        var fieldEditorMapper = _scope.Resolve<Mapping.IFieldEditorMapper>();

        var vm = new PresetEditorViewModel(
            presetUseCase,
            systemFieldUseCase,
            _dialogService,
            fieldEditorMapper,
            onSaved: () => { _ = NavigateToHomeAsync(); },
            onCancelled: () => { _ = NavigateToHomeAsync(); },
            existing: existing,
            seed: seed);

        vm.OnAnySuccessfulSave = () => { _ = SidebarViewModel?.LoadAsync(); };
        ResetBreadcrumb(LocalizationService.Instance["CollectionSettings"], vm);
        _ = vm.LoadAsync();
    }

    private void NavigateToTemplatePicker()
    {
        AppLogger.Log.Debug("Navigate: TemplatePicker");
        var library = _scope.Resolve<Templates.IPresetTemplateLibrary>();
        var vm = new PresetTemplatePickerViewModel(
            library,
            onTemplateChosen: seed => NavigateToPresetEditor(existing: null, seed: seed),
            onCancel: () => { _ = NavigateToHomeAsync(); });
        ResetBreadcrumb(LocalizationService.Instance["TemplatePickerTitle"], vm);
    }

    [RelayCommand]
    private void NavigateToSystemFieldLibrary()
    {
        AppLogger.Log.Debug("Navigate: SystemFieldLibrary");
        var systemFieldUseCase = _scope.Resolve<ISystemFieldUseCase>();
        var vm = new SystemFieldLibraryViewModel(
            systemFieldUseCase,
            _dialogService,
            _scope.Resolve<Mapping.IFieldEditorMapper>(),
            onDone: () => { _ = NavigateToHomeAsync(); });
        ResetBreadcrumb(LocalizationService.Instance["SystemFields"], vm);
        _ = vm.LoadAsync();
    }

    private void NavigateToItemEditor(Preset preset, EffectiveFields effectiveFields, Item? existing)
    {
        AppLogger.Log.Debug("Navigate: ItemEditor preset={PresetId} existing={ItemId} fields={Fields} groups={Groups}",
            preset.Id, existing?.Id, effectiveFields.Fields.Count, effectiveFields.Groups.Count);
        var itemUseCase = _scope.Resolve<IItemUseCase>();
        var presetUseCase = _scope.Resolve<IPresetUseCase>();

        var context = new ItemEditingContext(
            editorRegistry: _editorRegistry,
            listCellBuilder: _listCellBuilder,
            goBack: GoBack,
            pickAndStoreImageAsync: async () =>
            {
                var sp = TopLevel.GetTopLevel(Host)?.StorageProvider;
                if (sp is null) return null;
                var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    AllowMultiple = false,
                    FileTypeFilter = [FilePickerFileTypes.ImageAll]
                });
                var file = files.FirstOrDefault();
                if (file is null) return null;
                await using var stream = await file.OpenReadAsync();
                var key = await _imageStore.SaveAsync(stream, file.Name);
                using var previewStream = _imageStore.Open(key);
                return (key, file.Name, new Bitmap(previewStream));
            },
            exportImageAsync: async (key, suggestedFileName) =>
            {
                var sp = TopLevel.GetTopLevel(Host)?.StorageProvider;
                if (sp is null) return;
                var dest = await sp.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    SuggestedFileName = suggestedFileName
                });
                if (dest is null) return;
                using var src = _imageStore.Open(key);
                await using var dst = await dest.OpenWriteAsync();
                await src.CopyToAsync(dst);
            },
            loadImageBitmap: key =>
            {
                if (!_imageStore.Exists(key)) return null;
                using var stream = _imageStore.Open(key);
                return new Bitmap(stream);
            },
            deleteImageAsync: key => _imageStore.DeleteAsync(key));

        var barcodeDecoder = _scope.Resolve<IBarcodeImageDecoder>();
        context.ScanBarcodeAsync = async () =>
        {
            // Snapshot model: the user supplies a still image of the code (a photo or webcam frame
            // exported to a file); ZXing decodes it. Live in-app webcam capture is a future addition.
            var sp = TopLevel.GetTopLevel(Host)?.StorageProvider;
            if (sp is null) return null;
            var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                FileTypeFilter = [FilePickerFileTypes.ImageAll]
            });
            var file = files.FirstOrDefault();
            if (file is null) return null;
            await using var stream = await file.OpenReadAsync();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return barcodeDecoder.Decode(buffer.ToArray());
        };

        var qrGenerator = _scope.Resolve<IBarcodeImageGenerator>();
        context.GenerateQrBitmap = content =>
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            using var stream = new MemoryStream(qrGenerator.GenerateQrPng(content, 320));
            return new Bitmap(stream);
        };

        context.PickAndStoreFileAsync = async () =>
        {
            var sp = TopLevel.GetTopLevel(Host)?.StorageProvider;
            if (sp is null) return null;
            var files = await sp.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false });
            var file = files.FirstOrDefault();
            if (file is null) return null;
            await using var stream = await file.OpenReadAsync();
            var key = await _imageStore.SaveAsync(stream, file.Name);
            return (key, file.Name);
        };
        context.ExportFileAsync = async (key, suggestedFileName) =>
        {
            var sp = TopLevel.GetTopLevel(Host)?.StorageProvider;
            if (sp is null) return;
            var dest = await sp.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = suggestedFileName });
            if (dest is null) return;
            using var src = _imageStore.Open(key);
            await using var dst = await dest.OpenWriteAsync();
            await src.CopyToAsync(dst);
        };
        context.DeleteFileAsync = key => _imageStore.DeleteAsync(key);

        context.LoadLinkableItemsAsync = async () =>
        {
            var options = new List<LinkedItemOption>();
            foreach (var p in await presetUseCase.GetAllPresetsAsync())
            foreach (var it in await itemUseCase.GetItemsForPresetAsync(p.Id))
                if (existing is null || it.Id != existing.Id)
                    options.Add(new LinkedItemOption(it.Id, it.DisplayName));
            return options;
        };

        context.GlobalFieldLabelLayout = AppPreferences.Load().FieldLabelLayout;

        var rootEditor = new ItemEditorViewModel(
            itemUseCase,
            presetUseCase,
            preset,
            effectiveFields,
            onSaved: () => NavigateToPreset(preset),
            onCancelled: () => NavigateToPreset(preset),
            context: context,
            existing: existing);

        context.SaveAsync = rootEditor.PersistAsync;
        context.OpenList = listVm => PushBreadcrumb(listVm.Label, new ListDetailViewModel(listVm, context));
        context.OpenEntry = (entryVm, title) => PushBreadcrumb(title, entryVm);

        var title = string.IsNullOrWhiteSpace(existing?.DisplayName)
            ? LocalizationService.Instance["NewItem"]
            : existing!.DisplayName;
        PushBreadcrumb(title, rootEditor);
    }
}
