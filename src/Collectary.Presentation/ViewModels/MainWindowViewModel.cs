using System;
using System.Collections.Generic;
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
using Collectary.Presentation.ViewModels.Import;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.Presentation.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly ILifetimeScope _scope;
    private readonly IPresetUseCase _presetUseCase;
    private readonly IItemUseCase _itemUseCase;
    private readonly ISharedFieldUseCase _sharedFieldUseCase;
    private readonly IListCellBuilder _listCellBuilder;
    private readonly IFieldEditorRegistry _editorRegistry;
    private readonly IImageStore _imageStore;
    private readonly IDialogService _dialogService;
    private readonly ISyncScheduler _syncScheduler;

    public Visual? Host { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveFieldEditor))]
    public partial ViewModelBase? ContentViewModel { get; set; }

    public FieldListEditorViewModel? ActiveFieldEditor => ContentViewModel as FieldListEditorViewModel;

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    public partial string? CurrentProfileName { get; set; }

    [ObservableProperty]
    public partial ProfilePickerViewModel? ProfilePicker { get; set; }

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

    public IDialogHost? DialogHost { get; }

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
            navigateToSharedFields: NavigateToSharedFieldLibrary,
            pickFolder: PickSyncFolderAsync,
            onSyncChanged: OnSyncSettingsChanged,
            connectCloud: ConnectCloudAsync,
            pickCloudFolder: SetUpCloudFolderAsync,
            disconnectCloud: DisconnectCloudAsync,
            detectInstalledCloudFolder: () => new InstalledCloudFolderDetector().Detect(),
            exportBackup: ExportBackupAsync,
            importBackup: ImportBackupAsync,
            switchProfile: () => SwitchProfileCommand.Execute(null),
            audioRecorder: _scope.ResolveOptional<IAudioRecorder>(),
            audioPlayer: _scope.ResolveOptional<IAudioPlayer>());
        ResetBreadcrumb(LocalizationService.Instance["Settings"], vm);
        CloseSidebarIfNarrow();
    }

    private void CloseSidebarIfNarrow()
    {
        if (!IsNarrow) return;
        IsSidebarOpen = false;
        AppPreferences.Update(p => p with { SidebarOpen = false });
    }

    private async Task<bool> ExportBackupAsync()
    {
        var storage = TopLevel.GetTopLevel(Host)?.StorageProvider;
        if (storage is null)
        {
            AppLogger.Log.Warning("Backup export skipped: no storage provider on the current top level");
            return false;
        }
        var dest = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = "collection-backup.collectary",
            FileTypeChoices = [new FilePickerFileType("Collectary backup") { Patterns = ["*.collectary"] }],
        });
        if (dest is null) return false;

        await using var output = await dest.OpenWriteAsync();
        await _scope.Resolve<IBackupService>().ExportAsync(output);
        return true;
    }

    private async Task<BackupImportResult?> ImportBackupAsync()
    {
        var storage = TopLevel.GetTopLevel(Host)?.StorageProvider;
        if (storage is null)
        {
            AppLogger.Log.Warning("Backup import skipped: no storage provider on the current top level");
            return null;
        }
        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Collectary backup") { Patterns = ["*.collectary"] }],
        });
        var file = files.FirstOrDefault();
        if (file is null) return null;

        using var buffer = new MemoryStream();
        await using (var input = await file.OpenReadAsync())
            await input.CopyToAsync(buffer);
        buffer.Position = 0;

        var result = await _scope.Resolve<IBackupService>().ImportAsync(buffer);
        if (result.Applied > 0 && SidebarViewModel is not null)
            await SidebarViewModel.LoadAsync();
        return result;
    }

    private async Task<string?> ConnectCloudAsync(CloudProvider provider)
    {
        try
        {
            var auth = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudAuthClient>(_scope, provider);
            if (auth is null) return null;
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
        try
        {
            var rootProvider = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudRootProvider>(_scope, provider);
            var store = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudFileStore>(_scope, provider);
            if (rootProvider is null || store is null) return null;
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

    private async Task ShowCloudErrorAsync(Exception ex)
    {
        try
        {
            await _dialogService.ShowMessageAsync(
                $"{LocalizationService.Instance["Cloud_AuthFailed"]}\n\n{ex.Message}",
                LocalizationService.Instance["Settings"]);
        }
        catch (Exception dialogEx)
        {
            AppLogger.Log.Error(dialogEx, "Failed to show the cloud error dialog");
        }
    }

    private async Task DisconnectCloudAsync(CloudProvider provider)
    {
        try
        {
            var auth = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudAuthClient>(_scope, provider);
            if (auth is not null) await auth.SignOutAsync();
            Autofac.ResolutionExtensions.ResolveOptional<ISyncBackend>(_scope)?.Invalidate();
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Cloud sign-out failed for {Provider}", provider);
            await ShowCloudErrorAsync(ex);
        }
    }

    private void OnSyncSettingsChanged()
    {
        Autofac.ResolutionExtensions.ResolveOptional<ISyncBackend>(_scope)?.Invalidate();
        Sync.Refresh();
        ConfigureAutoSyncTimer(AppPreferences.Load());
        if (Sync.IsConfigured) _ = SyncThenReloadAsync();
    }

    public ObservableCollection<BreadcrumbNode> Breadcrumbs { get; } = new();

    public bool HasBreadcrumbs => Breadcrumbs.Count > 0;

    public ObservableCollection<BreadcrumbItem> BreadcrumbItems { get; } = new();

    private FieldListEditorViewModel? _trackedEditor;

    partial void OnContentViewModelChanged(ViewModelBase? oldValue, ViewModelBase? newValue)
    {
        if (oldValue is CameraScannerViewModel scanner)
            scanner.NotifyClosedExternally();

        if (_trackedEditor is not null)
            _trackedEditor.DrillBreadcrumbs.CollectionChanged -= OnDrillBreadcrumbsChanged;

        _trackedEditor = newValue as FieldListEditorViewModel;

        if (_trackedEditor is not null)
            _trackedEditor.DrillBreadcrumbs.CollectionChanged += OnDrillBreadcrumbsChanged;

        RebuildUnifiedTrail();
    }

    private void OnDrillBreadcrumbsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
        RebuildUnifiedTrail();

    private void RebuildUnifiedTrail()
    {
        var items = new List<BreadcrumbItem>
        {
            new(LocalizationService.Instance["MyCollections"], NavigateHomeCommand, null, isHome: true, isCurrent: false)
        };

        foreach (var node in Breadcrumbs)
            items.Add(new BreadcrumbItem(node.Title, NavigateToBreadcrumbCommand, node, isHome: false, isCurrent: false));

        if (_trackedEditor is not null)
            foreach (var level in _trackedEditor.DrillBreadcrumbs)
                items.Add(new BreadcrumbItem(level.Title, _trackedEditor.NavigateToLevelCommand, level, isHome: false, isCurrent: false));

        var last = items[^1];
        items[^1] = new BreadcrumbItem(last.Title, last.NavigateCommand, last.CommandParameter, last.IsHome, isCurrent: true);

        BreadcrumbItems.Clear();
        foreach (var item in items)
            BreadcrumbItems.Add(item);
    }

    partial void OnIsNarrowChanged(bool value)
    {
        RebuildUnifiedTrail();
        IsSidebarOpen = value ? false : AppPreferences.Load().SidebarOpen;
    }

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

    public async Task<bool> HandleSystemBackAsync()
    {
        if (ContentViewModel is ISystemBackHandler handler)
            return await handler.HandleSystemBackAsync();

        if (Breadcrumbs.Count > 1)
        {
            GoBack();
            return true;
        }

        if (ContentViewModel is not null and not WelcomeViewModel)
        {
            await NavigateToHomeAsync();
            return true;
        }

        return false;
    }

    [RelayCommand]
    private void NavigateToBreadcrumb(BreadcrumbNode node)
    {
        var index = Breadcrumbs.IndexOf(node);
        if (index < 0) return;
        while (Breadcrumbs.Count > index + 1)
            Breadcrumbs.RemoveAt(Breadcrumbs.Count - 1);
        ContentViewModel = node.Content;
        (node.Content as FieldListEditorViewModel)?.ResetToRoot();
    }

    public MainWindowViewModel(ILifetimeScope scope, IPresetUseCase presetUseCase, IItemUseCase itemUseCase, ISharedFieldUseCase sharedFieldUseCase, IListCellBuilder listCellBuilder, IFieldEditorRegistry editorRegistry, IImageStore imageStore, IDialogService dialogService, ISyncScheduler syncScheduler)
    {
        _scope = scope;
        _presetUseCase = presetUseCase;
        _itemUseCase = itemUseCase;
        _sharedFieldUseCase = sharedFieldUseCase;
        _listCellBuilder = listCellBuilder;
        _editorRegistry = editorRegistry;
        _imageStore = imageStore;
        _dialogService = dialogService;
        _syncScheduler = syncScheduler;
        DialogHost = dialogService as IDialogHost;
        Sync = new SyncViewModel(scope.Resolve<ISyncService>(), scope.Resolve<ISyncStatus>());
        Sync.Synced += OnSynced;
        Breadcrumbs.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasBreadcrumbs));
            RebuildUnifiedTrail();
        };
        RebuildUnifiedTrail();
    }

    private async Task<string?> PickSyncFolderAsync()
    {
        var storage = TopLevel.GetTopLevel(Host)?.StorageProvider;
        if (storage is null) return null;
        var folders = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        return folders.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task StartAsync()
    {
        var profiles = await _scope.Resolve<IProfileService>().GetProfilesAsync();
        var rememberedId = AppPreferences.Load().LastProfileId;
        var remembered = rememberedId is { } id ? profiles.FirstOrDefault(p => p.Id == id) : null;

        if (remembered is not null)
        {
            await EnterProfileAsync(remembered);
            return;
        }

        await ShowProfilePickerAsync();
    }

    private async Task ShowProfilePickerAsync()
    {
        var picker = new ProfilePickerViewModel(_scope.Resolve<IProfileService>(), OnProfileSelectedAsync);
        await picker.LoadAsync();
        ProfilePicker = picker;
        IsAuthenticated = false;
    }

    private async Task OnProfileSelectedAsync(User profile)
    {
        try
        {
            await EnterProfileAsync(profile);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Initialization after profile selection failed");
            await _dialogService.ShowMessageAsync(ex.Message, LocalizationService.Instance["Profile_PickTitle"]);
        }
    }

    private async Task EnterProfileAsync(User profile)
    {
        _scope.Resolve<IProfileService>().SelectProfile(profile);
        await _scope.Resolve<IAccountBootstrapper>().BackfillOwnerlessAsync(profile.Id);
        AppPreferences.Update(p => p with { LastProfileId = profile.Id });
        CurrentProfileName = profile.DisplayName;
        ProfilePicker = null;
        IsAuthenticated = true;
        await InitializeAsync();
    }

    [RelayCommand]
    private async Task SwitchProfile()
    {
        _scope.Resolve<IProfileService>().SignOut();
        Breadcrumbs.Clear();
        ContentViewModel = null;
        await ShowProfilePickerAsync();
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
        home.OnImportFromExcel = () => { _ = NavigateToExcelImportAsync(); };
        home.OnImportFromCsv = () => { _ = NavigateToCsvImportAsync(); };
        home.OnEditPreset = preset => NavigateToPresetEditor(preset);
        home.OnNavigateToSharedFields = NavigateToSharedFieldLibrary;
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
        if (Sync.IsConfigured) _ = RestoreThenSyncAsync();
    }

    private async Task RestoreThenSyncAsync()
    {
        await RestoreCloudSessionsAsync();
        await SyncThenReloadAsync();
    }

    private async Task RestoreCloudSessionsAsync()
    {
        foreach (var provider in new[] { CloudProvider.OneDrive, CloudProvider.GoogleDrive })
        {
            var auth = Autofac.ResolutionExtensions.ResolveOptionalKeyed<ICloudAuthClient>(_scope, provider);
            if (auth is null) continue;
            try
            {
                await auth.TryRestoreSessionAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                AppLogger.Log.Error(ex, "Cloud session restore failed for {Provider}", provider);
            }
        }
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
        if (_trackedEditor is not null)
            _trackedEditor.DrillBreadcrumbs.CollectionChanged -= OnDrillBreadcrumbsChanged;
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
            onTransferred: () => { _ = SidebarViewModel?.LoadAsync(); },
            onBack: () => { _ = NavigateToHomeAsync(); });
        _ = NavigateToShareAsync(vm);
    }

    private async Task NavigateToShareAsync(ShareDialogViewModel vm)
    {
        await vm.LoadAsync();
        ResetBreadcrumb(LocalizationService.Instance["Share_Title"], vm);
        CloseSidebarIfNarrow();
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

        CloseSidebarIfNarrow();
    }

    private void NavigateToPresetEditor(Preset? existing, Preset? seed = null)
    {
        AppLogger.Log.Debug("Navigate: PresetEditor existing={Id} seed={Seed}", existing?.Id, seed?.Name);
        var presetUseCase = _scope.Resolve<IPresetUseCase>();
        var sharedFieldUseCase = _scope.Resolve<ISharedFieldUseCase>();
        var fieldEditorMapper = _scope.Resolve<Mapping.IFieldEditorMapper>();

        var vm = new PresetEditorViewModel(
            presetUseCase,
            sharedFieldUseCase,
            _dialogService,
            fieldEditorMapper,
            onSaved: () => { _ = NavigateToHomeAsync(); },
            onCancelled: () => { _ = NavigateToHomeAsync(); },
            existing: existing,
            seed: seed);

        vm.OnAnySuccessfulSave = () => { _ = SidebarViewModel?.LoadAsync(); };
        ResetBreadcrumb(LocalizationService.Instance["CollectionSettings"], vm);
        _ = vm.LoadAsync();

        CloseSidebarIfNarrow();
    }

    private Task NavigateToExcelImportAsync() =>
        ImportFromFileAsync(
            new FilePickerFileType(LocalizationService.Instance["Import_FileType_Excel"]) { Patterns = ["*.xlsx"] },
            stream => _scope.Resolve<IExcelWorkbookReader>().Read(stream),
            "Import_Excel_Title");

    private Task NavigateToCsvImportAsync() =>
        ImportFromFileAsync(
            new FilePickerFileType(LocalizationService.Instance["Import_FileType_Csv"]) { Patterns = ["*.csv"] },
            stream => _scope.Resolve<ICsvWorkbookReader>().Read(stream),
            "Import_Csv_Title");

    private async Task ImportFromFileAsync(
        FilePickerFileType fileType,
        Func<Stream, Core.Domain.Import.WorkbookData> read,
        string breadcrumbTitleKey)
    {
        var storage = TopLevel.GetTopLevel(Host)?.StorageProvider;
        if (storage is null) return;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [fileType],
        });
        var file = files.FirstOrDefault();
        if (file is null) return;

        Core.Domain.Import.WorkbookData data;
        try
        {
            using var buffer = new MemoryStream();
            await CopyFileToAsync(file, buffer);
            buffer.Position = 0;
            data = read(buffer);
        }
        catch (Exception ex)
        {
            AppLogger.Log.Error(ex, "Import file read failed");
            await _dialogService.ShowMessageAsync(
                LocalizationService.Instance["Import_ReadFailed"], LocalizationService.Instance["Import_Title"]);
            return;
        }

        if (data.Sheets.Count == 0)
        {
            await _dialogService.ShowMessageAsync(
                LocalizationService.Instance["Import_NoSheets"], LocalizationService.Instance["Import_Title"]);
            return;
        }

        var presets = await _presetUseCase.GetWritablePresetsAsync();
        var vm = new ExcelImportViewModel(
            data,
            _scope.Resolve<IGridShaper>(),
            _scope.Resolve<ICultureDetector>(),
            _scope.Resolve<IFieldTypeInference>(),
            _scope.Resolve<ISpreadsheetImportService>(),
            _presetUseCase,
            _dialogService,
            presets,
            onFinished: async preset =>
            {
                if (SidebarViewModel is not null) await SidebarViewModel.LoadAsync();
                NavigateToPreset(preset);
            },
            onClose: () => { _ = NavigateToHomeAsync(); });

        ResetBreadcrumb(LocalizationService.Instance[breadcrumbTitleKey], vm);
        CloseSidebarIfNarrow();
    }

    private async Task CopyFileToAsync(IStorageFile file, Stream destination)
    {
        try
        {
            await using var input = await file.OpenReadAsync();
            await input.CopyToAsync(destination);
        }
        catch (IOException) when (file.TryGetLocalPath() is { } localPath)
        {
            await using var input = new FileStream(
                localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            await input.CopyToAsync(destination);
        }
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
        CloseSidebarIfNarrow();
    }

    [RelayCommand]
    private void NavigateToSharedFieldLibrary()
    {
        AppLogger.Log.Debug("Navigate: SharedFieldLibrary");
        var sharedFieldUseCase = _scope.Resolve<ISharedFieldUseCase>();
        var vm = new SharedFieldLibraryViewModel(
            sharedFieldUseCase,
            _dialogService,
            _scope.Resolve<Mapping.IFieldEditorMapper>(),
            onDone: () => { _ = NavigateToHomeAsync(); });
        ResetBreadcrumb(LocalizationService.Instance["SharedFields"], vm);
        _ = vm.LoadAsync();
        CloseSidebarIfNarrow();
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

        context.Dialogs = _dialogService;

        var barcodeDecoder = _scope.Resolve<IBarcodeImageDecoder>();
        context.ScanBarcodeAsync = async () =>
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
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return barcodeDecoder.Decode(buffer.ToArray());
        };

        var permissions = _scope.ResolveOptional<IRuntimePermissions>();
        if (permissions is not null)
            context.RequestPermissionAsync = permissions.RequestAsync;

        var liveCamera = _scope.ResolveOptional<ILiveCamera>();
        context.IsCameraScanAvailableAsync = () =>
            liveCamera is null
                ? Task.FromResult(false)
                : Task.Run(() => liveCamera.GetDevices().Count > 0);
        context.ScanBarcodeFromCameraAsync = () =>
        {
            if (liveCamera is null) return Task.FromResult<BarcodeReadResult?>(null);
            var tcs = new TaskCompletionSource<BarcodeReadResult?>();
            var scanner = new CameraScannerViewModel(liveCamera, barcodeDecoder, _dialogService,
                () => context.RequestPermissionAsync(RuntimePermission.Camera),
                result => tcs.TrySetResult(result),
                navigateBack: GoBack);
            PushBreadcrumb(LocalizationService.Instance["Barcode_CameraScanner"], scanner);
            CloseSidebarIfNarrow();
            return tcs.Task;
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

        context.AudioRecorder = _scope.ResolveOptional<IAudioRecorder>();
        context.AudioPlayer = _scope.ResolveOptional<IAudioPlayer>();
        context.ResolveAudioInputDeviceId = () => AppPreferences.Load().AudioInputDeviceId;
        context.ResolveAudioOutputDeviceId = () => AppPreferences.Load().AudioOutputDeviceId;
        context.OpenSettings = NavigateToSettings;
        context.StoreAudioAsync = stream => _imageStore.SaveAsync(stream, $"audio-{Guid.NewGuid():N}.wav");
        context.OpenAudioStream = key => _imageStore.Exists(key) ? _imageStore.Open(key) : null;

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
