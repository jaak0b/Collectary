using Autofac;
using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Presentation.DI;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.Mapping;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class MainWindowViewModelTest
{
    private IContainer _container = null!;
    private ILifetimeScope _scope = null!;
    private IPresetUseCase _presetUseCase = null!;
    private IItemUseCase _itemUseCase = null!;
    private ISharedFieldUseCase _sharedFieldUseCase = null!;
    private IListCellBuilder _listCellBuilder = null!;
    private IFieldEditorRegistry _editorRegistry = null!;
    private IImageStore _imageStore = null!;
    private IDialogService _dialogService = null!;
    private ISyncScheduler _syncScheduler = null!;
    private IProfileService _profileService = null!;
    private IAccountBootstrapper _accountBootstrapper = null!;
    private IShareUseCase _shareUseCase = null!;
    private string _prefsDir = null!;
    private string _originalPrefs = null!;

    [SetUp]
    public void SetUp()
    {
        _presetUseCase = A.Fake<IPresetUseCase>();
        _itemUseCase = A.Fake<IItemUseCase>();
        _sharedFieldUseCase = A.Fake<ISharedFieldUseCase>();
        _listCellBuilder = A.Fake<IListCellBuilder>();
        _editorRegistry = A.Fake<IFieldEditorRegistry>();
        _imageStore = A.Fake<IImageStore>();
        _dialogService = A.Fake<IDialogService>();
        _syncScheduler = A.Fake<ISyncScheduler>();
        _profileService = A.Fake<IProfileService>();
        _accountBootstrapper = A.Fake<IAccountBootstrapper>();
        _shareUseCase = A.Fake<IShareUseCase>();

        _prefsDir = Path.Combine(Path.GetTempPath(), $"collectary-prefs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_prefsDir);
        _originalPrefs = AppPreferences.FilePath;
        AppPreferences.FilePath = Path.Combine(_prefsDir, "preferences.json");

        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset>());
        A.CallTo(() => _sharedFieldUseCase.GetAllAsync()).Returns((IReadOnlyList<SharedField>)new List<SharedField>());
        A.CallTo(() => _shareUseCase.ListSharesAsync(A<Guid>._)).Returns(new List<ShareInfo>());
        A.CallTo(() => _profileService.GetProfilesAsync()).Returns(new List<User>());

        var builder = new ContainerBuilder();
        builder.RegisterInstance(A.Fake<ISyncService>()).As<ISyncService>();
        builder.RegisterInstance(A.Fake<ISyncStatus>()).As<ISyncStatus>();
        builder.RegisterInstance(new Infrastructure.InlineUiDispatcher())
            .As<Collectary.Presentation.Services.IUiDispatcher>();
        builder.RegisterInstance(new Infrastructure.InlineBackgroundRunner())
            .As<Collectary.Presentation.Services.IBackgroundRunner>();
        builder.RegisterInstance(_presetUseCase).As<IPresetUseCase>();
        builder.RegisterInstance(_sharedFieldUseCase).As<ISharedFieldUseCase>();
        builder.RegisterInstance(_profileService).As<IProfileService>();
        builder.RegisterInstance(_accountBootstrapper).As<IAccountBootstrapper>();
        builder.RegisterInstance(_shareUseCase).As<IShareUseCase>();
        builder.RegisterInstance(new TestFieldEditorMapper().Create()).As<IFieldEditorMapper>();
        builder.RegisterInstance(A.Fake<Collectary.Presentation.Templates.IPresetTemplateLibrary>())
            .As<Collectary.Presentation.Templates.IPresetTemplateLibrary>();
        _container = builder.Build();
        _scope = _container.BeginLifetimeScope();
    }

    [TearDown]
    public void TearDown()
    {
        _syncScheduler.Dispose();
        _scope.Dispose();
        _container.Dispose();
        AppPreferences.FilePath = _originalPrefs;
        if (Directory.Exists(_prefsDir)) Directory.Delete(_prefsDir, recursive: true);
    }

    private MainWindowViewModel CreateSut() => new(
        _scope,
        _presetUseCase,
        _itemUseCase,
        _sharedFieldUseCase,
        _listCellBuilder,
        _editorRegistry,
        _imageStore,
        _dialogService,
        _syncScheduler);

    [Test]
    public void IsDebugBuild_MatchesTheBuildConfiguration()
    {
        var sut = CreateSut();

#if DEBUG
        Assert.That(sut.IsDebugBuild, Is.True);
#else
        Assert.That(sut.IsDebugBuild, Is.False);
#endif
    }

    [Test]
    public void IsMobileSidebarVisible_WhenNarrowAndOpen_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        Assert.That(sut.IsMobileSidebarVisible, Is.True);
    }

    [Test]
    public void IsMobileSidebarVisible_WhenWideAndOpen_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        Assert.That(sut.IsMobileSidebarVisible, Is.False);
    }

    [Test]
    public void BecomingNarrow_WhileSidebarOpen_AutoCollapsesSidebar()
    {
        var sut = CreateSut();
        sut.IsSidebarOpen = true;

        sut.IsNarrow = true;

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public void BecomingNarrow_DoesNotOverwriteSavedSidebarPreference()
    {
        AppPreferences.Update(p => p with { SidebarOpen = true });
        var sut = CreateSut();
        sut.IsSidebarOpen = true;

        sut.IsNarrow = true;

        Assert.That(AppPreferences.Load().SidebarOpen, Is.True);
    }

    [Test]
    public void ReturningToWide_RestoresSidebarFromPreference()
    {
        AppPreferences.Update(p => p with { SidebarOpen = true });
        var sut = CreateSut();
        sut.IsNarrow = true;

        sut.IsNarrow = false;

        Assert.That(sut.IsSidebarOpen, Is.True);
    }

    [Test]
    public void IsMobileSidebarVisible_WhenNarrowAndClosed_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = false;

        Assert.That(sut.IsMobileSidebarVisible, Is.False);
    }

    [Test]
    public void IsDesktopSidebarVisible_WhenWideAndOpen_ReturnsTrue()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        Assert.That(sut.IsDesktopSidebarVisible, Is.True);
    }

    [Test]
    public void IsDesktopSidebarVisible_WhenNarrowAndOpen_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        Assert.That(sut.IsDesktopSidebarVisible, Is.False);
    }

    [Test]
    public void IsDesktopSidebarVisible_WhenWideAndClosed_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = false;

        Assert.That(sut.IsDesktopSidebarVisible, Is.False);
    }

    [Test]
    public void IsMobileSidebarVisible_RaisesPropertyChanged_WhenIsNarrowChanges()
    {
        var sut = CreateSut();
        sut.IsSidebarOpen = true;
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.IsNarrow = true;

        Assert.That(raised, Does.Contain(nameof(MainWindowViewModel.IsMobileSidebarVisible)));
    }

    [Test]
    public void BreadcrumbItems_Initially_ContainsOnlyHome()
    {
        var sut = CreateSut();

        Assert.That(sut.BreadcrumbItems, Has.Count.EqualTo(1));
        Assert.That(sut.BreadcrumbItems[0].IsHome, Is.True);
    }

    [Test]
    public void BreadcrumbItems_ComposesHomeThenMainCrumbs_InOrder()
    {
        var sut = CreateSut();
        sut.Breadcrumbs.Add(new BreadcrumbNode("A", A.Fake<ViewModelBase>()));
        sut.Breadcrumbs.Add(new BreadcrumbNode("B", A.Fake<ViewModelBase>()));

        Assert.That(sut.BreadcrumbItems[0].IsHome, Is.True);
        Assert.That(sut.BreadcrumbItems.Skip(1).Select(i => i.Title), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void BreadcrumbItems_LastItem_IsCurrent()
    {
        var sut = CreateSut();
        sut.Breadcrumbs.Add(new BreadcrumbNode("A", A.Fake<ViewModelBase>()));
        sut.Breadcrumbs.Add(new BreadcrumbNode("B", A.Fake<ViewModelBase>()));

        Assert.That(sut.BreadcrumbItems[^1].IsCurrent, Is.True);
        Assert.That(sut.BreadcrumbItems.Take(sut.BreadcrumbItems.Count - 1).Any(i => i.IsCurrent), Is.False);
    }

    [Test]
    public void BreadcrumbItems_MainItem_RoutesToNavigateToBreadcrumbCommand()
    {
        var sut = CreateSut();
        var node = new BreadcrumbNode("A", A.Fake<ViewModelBase>());
        sut.Breadcrumbs.Add(node);

        var item = sut.BreadcrumbItems.Single(i => !i.IsHome);
        Assert.That(item.NavigateCommand, Is.SameAs(sut.NavigateToBreadcrumbCommand));
        Assert.That(item.CommandParameter, Is.SameAs(node));
    }

    [Test]
    public void BreadcrumbItems_AddingNode_GrowsCollection()
    {
        var sut = CreateSut();
        var before = sut.BreadcrumbItems.Count;

        sut.Breadcrumbs.Add(new BreadcrumbNode("A", A.Fake<ViewModelBase>()));

        Assert.That(sut.BreadcrumbItems.Count, Is.EqualTo(before + 1));
    }

    [Test]
    public void BreadcrumbItems_WhenEditorDrills_AppendsDrillLevelsRoutedToLevelCommand()
    {
        var sut = CreateSut();
        var editor = CreateEditor();
        sut.Breadcrumbs.Add(new BreadcrumbNode("Settings", editor));
        sut.ContentViewModel = editor;
        editor.AddGroupCommand.Execute(null);
        var group = editor.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        editor.DrillIntoCommand.Execute(group);

        Assert.That(sut.BreadcrumbItems.Count, Is.GreaterThanOrEqualTo(3));
        var last = sut.BreadcrumbItems[^1];
        Assert.That(last.IsCurrent, Is.True);
        Assert.That(last.NavigateCommand, Is.SameAs(editor.NavigateToLevelCommand));
    }

    [Test]
    public void BreadcrumbItems_WhenEditorSwappedAway_StopsTrackingItsDrill()
    {
        var sut = CreateSut();
        var editor = CreateEditor();
        sut.ContentViewModel = editor;
        sut.ContentViewModel = new WelcomeViewModel();
        var before = sut.BreadcrumbItems.Count;

        editor.AddGroupCommand.Execute(null);
        var group = editor.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        editor.DrillIntoCommand.Execute(group);

        Assert.That(sut.BreadcrumbItems.Count, Is.EqualTo(before));
    }

    [Test]
    public void IsDesktopSidebarVisible_RaisesPropertyChanged_WhenIsSidebarOpenChanges()
    {
        var sut = CreateSut();
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.IsSidebarOpen = true;

        Assert.That(raised, Does.Contain(nameof(MainWindowViewModel.IsDesktopSidebarVisible)));
    }

    [Test]
    public async Task NavigateToPresetEditor_WhenNarrow_ClosesSidebar()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnCreatePreset?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public async Task NavigateToPresetEditor_WhenWide_LeavesSidebarOpen()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnCreatePreset?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.True);
    }

    [Test]
    public async Task NavigateToTemplatePicker_WhenNarrow_ClosesSidebar()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnCreateFromTemplate?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public async Task NavigateToTemplatePicker_WhenWide_LeavesSidebarOpen()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnCreateFromTemplate?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.True);
    }

    [Test]
    public async Task NavigateToSharedFieldLibrary_WhenNarrow_ClosesSidebar()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnNavigateToSharedFields?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public async Task NavigateToSharedFieldLibrary_WhenWide_LeavesSidebarOpen()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnNavigateToSharedFields?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.True);
    }

    [Test]
    public async Task NavigateToSettings_WhenNarrow_ClosesSidebar()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        sut.NavigateToSettingsCommand.Execute(null);

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public async Task NavigateToSettings_WhenWide_LeavesSidebarOpen()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        sut.NavigateToSettingsCommand.Execute(null);

        Assert.That(sut.IsSidebarOpen, Is.True);
    }

    [Test]
    public async Task StartAsync_WithNoProfiles_ShowsPickerAndIsNotAuthenticated()
    {
        var sut = CreateSut();

        await sut.StartAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.ProfilePicker, Is.Not.Null);
            Assert.That(sut.IsAuthenticated, Is.False);
            Assert.That(sut.SidebarViewModel, Is.Null);
        });
    }

    [Test]
    public async Task StartAsync_WithRememberedProfile_EntersApp()
    {
        var profile = new User { Username = "alice", DisplayName = "Alice" };
        A.CallTo(() => _profileService.GetProfilesAsync()).Returns(new List<User> { profile });
        AppPreferences.Update(p => p with { LastProfileId = profile.Id });
        var sut = CreateSut();

        await sut.StartAsync();

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.True);
            Assert.That(sut.ProfilePicker, Is.Null);
            Assert.That(sut.CurrentProfileName, Is.EqualTo("Alice"));
            Assert.That(sut.SidebarViewModel, Is.Not.Null);
        });
        A.CallTo(() => _profileService.SelectProfile(profile)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task StartAsync_WhenRememberedProfileMissing_ShowsPicker()
    {
        A.CallTo(() => _profileService.GetProfilesAsync()).Returns(new List<User>());
        AppPreferences.Update(p => p with { LastProfileId = Guid.NewGuid() });
        var sut = CreateSut();

        await sut.StartAsync();

        Assert.That(sut.ProfilePicker, Is.Not.Null);
        Assert.That(sut.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task OnProfileSelected_FromPicker_PersistsAndInitializes()
    {
        var profile = new User { Username = "alice", DisplayName = "Alice" };
        A.CallTo(() => _profileService.GetProfilesAsync()).Returns(new List<User> { profile });
        var sut = CreateSut();
        await sut.StartAsync();
        var tile = sut.ProfilePicker!.Profiles.Single();

        await sut.ProfilePicker!.SelectProfileCommand.ExecuteAsync(tile);

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.True);
            Assert.That(sut.ProfilePicker, Is.Null);
            Assert.That(sut.SidebarViewModel, Is.Not.Null);
            Assert.That(AppPreferences.Load().LastProfileId, Is.EqualTo(profile.Id));
        });
        A.CallTo(() => _accountBootstrapper.BackfillOwnerlessAsync(profile.Id)).MustHaveHappenedOnceExactly();
    }

    private PresetEditorViewModel CreateEditor() => new(
        _presetUseCase,
        _sharedFieldUseCase,
        _dialogService,
        new TestFieldEditorMapper().Create(),
        onSaved: () => { },
        onCancelled: () => { });

    [Test]
    public void ActiveFieldEditor_WhenContentIsFieldEditor_ReturnsIt()
    {
        var sut = CreateSut();
        var editor = CreateEditor();

        sut.ContentViewModel = editor;

        Assert.That(sut.ActiveFieldEditor, Is.SameAs(editor));
    }

    [Test]
    public void SwappingAwayFromCameraScanner_ClosesItWithNullResultAndStopsCamera()
    {
        var sut = CreateSut();
        var camera = A.Fake<ILiveCamera>();
        A.CallTo(() => camera.GetDevices()).Returns(new[] { new CameraDevice("0", "Front") });
        A.CallTo(() => camera.StopAsync()).Returns(Task.CompletedTask);
        BarcodeReadResult? result = new("x", BarcodeSymbology.QrCode);
        var resultDelivered = false;
        var scanner = new CameraScannerViewModel(camera, A.Fake<IBarcodeImageDecoder>(), _dialogService,
            () => Task.FromResult(true),
            r => { resultDelivered = true; result = r; }, () => { });
        sut.ContentViewModel = scanner;

        sut.ContentViewModel = new WelcomeViewModel();

        Assert.Multiple(() =>
        {
            Assert.That(resultDelivered, Is.True);
            Assert.That(result, Is.Null);
        });
        A.CallTo(() => camera.StopAsync()).MustHaveHappened();
    }

    [Test]
    public void ActiveFieldEditor_WhenContentIsNotFieldEditor_ReturnsNull()
    {
        var sut = CreateSut();

        sut.ContentViewModel = new WelcomeViewModel();

        Assert.That(sut.ActiveFieldEditor, Is.Null);
    }

    [Test]
    public void ActiveFieldEditor_RaisesPropertyChanged_WhenContentChanges()
    {
        var sut = CreateSut();
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.ContentViewModel = CreateEditor();

        Assert.That(raised, Does.Contain(nameof(MainWindowViewModel.ActiveFieldEditor)));
    }

    [Test]
    public void NavigateToBreadcrumb_WhenContentIsDrilledEditor_ResetsEditorToRoot()
    {
        var sut = CreateSut();
        var editor = CreateEditor();
        editor.AddGroupCommand.Execute(null);
        var group = editor.CurrentRows.OfType<FieldGroupRowViewModel>().First();
        editor.DrillIntoCommand.Execute(group);
        var node = new BreadcrumbNode("Collection Settings", editor);
        sut.Breadcrumbs.Add(node);

        sut.NavigateToBreadcrumbCommand.Execute(node);

        Assert.That(editor.Levels.Count, Is.EqualTo(1));
        Assert.That(editor.DrillBreadcrumbs, Is.Empty);
    }

    private async Task<MainWindowViewModel> EnteredAppWithProfileAsync(User profile)
    {
        A.CallTo(() => _profileService.GetProfilesAsync()).Returns(new List<User> { profile });
        A.CallTo(() => _profileService.CurrentProfile).Returns(profile);
        AppPreferences.Update(p => p with { LastProfileId = profile.Id });
        var sut = CreateSut();
        await sut.StartAsync();
        sut.NavigateToSettingsCommand.Execute(null);
        return sut;
    }

    [Test]
    public async Task DeleteProfile_OnConfirm_DeletesProfileAndReturnsToPicker()
    {
        var profile = new User { Username = "alice", DisplayName = "Alice" };
        A.CallTo(() => _profileService.CountOwnedCollectionsAsync()).Returns(2);
        A.CallTo(() => _dialogService.ConfirmAsync(A<string>._, A<string>._, A<string>._)).Returns(true);
        var sut = await EnteredAppWithProfileAsync(profile);
        var settings = (SettingsViewModel)sut.ContentViewModel!;

        await settings.DeleteProfileCommand.ExecuteAsync(null);

        A.CallTo(() => _profileService.DeleteCurrentProfileAsync()).MustHaveHappenedOnceExactly();
        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.False);
            Assert.That(sut.ProfilePicker, Is.Not.Null);
        });
    }

    [Test]
    public async Task DeleteProfile_OnCancel_DoesNotDeleteAndStaysSignedIn()
    {
        var profile = new User { Username = "alice", DisplayName = "Alice" };
        A.CallTo(() => _dialogService.ConfirmAsync(A<string>._, A<string>._, A<string>._)).Returns(false);
        var sut = await EnteredAppWithProfileAsync(profile);
        var settings = (SettingsViewModel)sut.ContentViewModel!;

        await settings.DeleteProfileCommand.ExecuteAsync(null);

        A.CallTo(() => _profileService.DeleteCurrentProfileAsync()).MustNotHaveHappened();
        Assert.That(sut.IsAuthenticated, Is.True);
    }

    [Test]
    public async Task SwitchProfile_SignsOutAndShowsPicker()
    {
        var profile = new User { Username = "alice", DisplayName = "Alice" };
        A.CallTo(() => _profileService.GetProfilesAsync()).Returns(new List<User> { profile });
        AppPreferences.Update(p => p with { LastProfileId = profile.Id });
        var sut = CreateSut();
        await sut.StartAsync();

        await sut.SwitchProfileCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.False);
            Assert.That(sut.ProfilePicker, Is.Not.Null);
            Assert.That(sut.Breadcrumbs, Is.Empty);
        });
        A.CallTo(() => _profileService.SignOut()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SharePreset_SetsContentViewModelToShareDialogViewModel()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        var preset = new Preset { Id = Guid.NewGuid(), Name = "My collection" };

        sut.SidebarViewModel!.OnSharePreset?.Invoke(preset);

        Assert.That(sut.ContentViewModel, Is.InstanceOf<ShareDialogViewModel>());
    }

    [Test]
    public async Task SharePreset_WhenNarrow_ClosesSidebar()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;
        var preset = new Preset { Id = Guid.NewGuid(), Name = "My collection" };

        sut.SidebarViewModel!.OnSharePreset?.Invoke(preset);

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public async Task SharePreset_WhenWide_LeavesSidebarOpen()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;
        var preset = new Preset { Id = Guid.NewGuid(), Name = "My collection" };

        sut.SidebarViewModel!.OnSharePreset?.Invoke(preset);

        Assert.That(sut.IsSidebarOpen, Is.True);
    }

    private sealed class StubBackContent : ViewModelBase, ISystemBackHandler
    {
        public int Calls { get; private set; }
        public bool Result { get; init; } = true;

        public Task<bool> HandleSystemBackAsync()
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }

    private sealed class PlainContent : ViewModelBase;

    [Test]
    public async Task HandleSystemBackAsync_WhenContentHandlesBack_DelegatesAndReturnsTrue()
    {
        var sut = CreateSut();
        var content = new StubBackContent { Result = true };
        sut.ContentViewModel = content;

        var handled = await sut.HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(content.Calls, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleSystemBackAsync_WhenContentDeclinesBack_ReturnsFalse()
    {
        var sut = CreateSut();
        sut.ContentViewModel = new StubBackContent { Result = false };

        Assert.That(await sut.HandleSystemBackAsync(), Is.False);
    }

    [Test]
    public async Task HandleSystemBackAsync_WithNestedBreadcrumbs_PopsToPreviousAndReturnsTrue()
    {
        var sut = CreateSut();
        var first = new PlainContent();
        var second = new PlainContent();
        sut.Breadcrumbs.Add(new BreadcrumbNode("first", first));
        sut.Breadcrumbs.Add(new BreadcrumbNode("second", second));
        sut.ContentViewModel = second;

        var handled = await sut.HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(sut.ContentViewModel, Is.SameAs(first));
            Assert.That(sut.Breadcrumbs, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleSystemBackAsync_AtTopLevelNonHome_NavigatesHomeAndReturnsTrue()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.ContentViewModel = new PlainContent();

        var handled = await sut.HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(sut.ContentViewModel, Is.InstanceOf<WelcomeViewModel>());
        });
    }

    [Test]
    public async Task HandleSystemBackAsync_AtHome_PromptsToConfirmExit()
    {
        var sut = CreateSut();
        sut.ContentViewModel = new WelcomeViewModel();

        await sut.HandleSystemBackAsync();

        A.CallTo(() => _dialogService.ConfirmAsync(A<string>._, A<string>._, A<string>._)).MustHaveHappened();
    }

    [Test]
    public async Task HandleSystemBackAsync_AtHome_WhenUserConfirmsExit_ReturnsFalseSoTheOsCanExit()
    {
        var sut = CreateSut();
        sut.ContentViewModel = new WelcomeViewModel();
        A.CallTo(() => _dialogService.ConfirmAsync(A<string>._, A<string>._, A<string>._)).Returns(true);

        Assert.That(await sut.HandleSystemBackAsync(), Is.False);
    }

    [Test]
    public async Task HandleSystemBackAsync_AtHome_WhenUserCancelsExit_ReturnsTrueAndStaysInApp()
    {
        var sut = CreateSut();
        sut.ContentViewModel = new WelcomeViewModel();
        A.CallTo(() => _dialogService.ConfirmAsync(A<string>._, A<string>._, A<string>._)).Returns(false);

        Assert.That(await sut.HandleSystemBackAsync(), Is.True);
    }

    [Test]
    public async Task HandleSystemBackAsync_AtHome_SurfacesExitConfirmationThroughRealDialogService()
    {
        var dialogs = new OverlayDialogService();
        var sut = new MainWindowViewModel(
            _scope, _presetUseCase, _itemUseCase, _sharedFieldUseCase,
            _listCellBuilder, _editorRegistry, _imageStore, dialogs, _syncScheduler)
        {
            ContentViewModel = new WelcomeViewModel()
        };

        var back = sut.HandleSystemBackAsync();
        var dialog = (ConfirmDialogViewModel)dialogs.ActiveDialog!;

        Assert.Multiple(() =>
        {
            Assert.That(dialogs.HasActiveDialog, Is.True);
            Assert.That(dialog.ConfirmLabel, Is.EqualTo(Collectary.Presentation.Localization.LocalizationService.Instance["ConfirmExitConfirm"]));
        });

        dialog.ConfirmCommand.Execute(null);

        Assert.That(await back, Is.False);
    }

    [Test]
    public async Task HandleSystemBackAsync_WhileLayersRemain_NeverPromptsToConfirmExit()
    {
        var sut = CreateSut();
        var first = new PlainContent();
        var second = new PlainContent();
        sut.Breadcrumbs.Add(new BreadcrumbNode("first", first));
        sut.Breadcrumbs.Add(new BreadcrumbNode("second", second));
        sut.ContentViewModel = second;

        var handled = await sut.HandleSystemBackAsync();

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            A.CallTo(() => _dialogService.ConfirmAsync(A<string>._, A<string>._, A<string>._)).MustNotHaveHappened();
        });
    }
}
