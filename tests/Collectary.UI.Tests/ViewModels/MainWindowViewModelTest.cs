using Autofac;
using FakeItEasy;
using Collectary.Core.Domain;
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
    private ISystemFieldUseCase _systemFieldUseCase = null!;
    private IListCellBuilder _listCellBuilder = null!;
    private IFieldEditorRegistry _editorRegistry = null!;
    private IImageStore _imageStore = null!;
    private IDialogService _dialogService = null!;
    private ISyncScheduler _syncScheduler = null!;
    private IAuthService _authService = null!;
    private IAccountBootstrapper _accountBootstrapper = null!;
    private IShareUseCase _shareUseCase = null!;

    [SetUp]
    public void SetUp()
    {
        _presetUseCase = A.Fake<IPresetUseCase>();
        _itemUseCase = A.Fake<IItemUseCase>();
        _systemFieldUseCase = A.Fake<ISystemFieldUseCase>();
        _listCellBuilder = A.Fake<IListCellBuilder>();
        _editorRegistry = A.Fake<IFieldEditorRegistry>();
        _imageStore = A.Fake<IImageStore>();
        _dialogService = A.Fake<IDialogService>();
        _syncScheduler = A.Fake<ISyncScheduler>();
        _authService = A.Fake<IAuthService>();
        _accountBootstrapper = A.Fake<IAccountBootstrapper>();
        _shareUseCase = A.Fake<IShareUseCase>();

        A.CallTo(() => _presetUseCase.GetAllPresetsAsync()).Returns(new List<Preset>());
        A.CallTo(() => _systemFieldUseCase.GetAllAsync()).Returns((IReadOnlyList<SystemField>)new List<SystemField>());
        A.CallTo(() => _shareUseCase.ListSharesAsync(A<Guid>._)).Returns(new List<ShareInfo>());

        var builder = new ContainerBuilder();
        builder.RegisterInstance(A.Fake<ISyncService>()).As<ISyncService>();
        builder.RegisterInstance(A.Fake<ISyncStatus>()).As<ISyncStatus>();
        builder.RegisterInstance(_presetUseCase).As<IPresetUseCase>();
        builder.RegisterInstance(_systemFieldUseCase).As<ISystemFieldUseCase>();
        builder.RegisterInstance(_authService).As<IAuthService>();
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
    }

    private MainWindowViewModel CreateSut() => new(
        _scope,
        _presetUseCase,
        _itemUseCase,
        _systemFieldUseCase,
        _listCellBuilder,
        _editorRegistry,
        _imageStore,
        _dialogService,
        _syncScheduler);

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
    public void Breadcrumbs_WhenWithinWideLimit_AllVisibleNoneCollapsed()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        sut.Breadcrumbs.Add(new BreadcrumbNode("A", A.Fake<ViewModelBase>()));
        sut.Breadcrumbs.Add(new BreadcrumbNode("B", A.Fake<ViewModelBase>()));

        Assert.That(sut.VisibleBreadcrumbs.Select(n => n.Title), Is.EqualTo(new[] { "A", "B" }));
        Assert.That(sut.HasCollapsedBreadcrumbs, Is.False);
    }

    [Test]
    public void Breadcrumbs_WhenWideAndDeep_CollapsesLeadingKeepsTrailing()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        foreach (var t in new[] { "A", "B", "C", "D", "E" })
            sut.Breadcrumbs.Add(new BreadcrumbNode(t, A.Fake<ViewModelBase>()));

        Assert.That(sut.HasCollapsedBreadcrumbs, Is.True);
        Assert.That(sut.VisibleBreadcrumbs.Last().Title, Is.EqualTo("E"));
        Assert.That(sut.CollapsedBreadcrumbs.First().Title, Is.EqualTo("A"));
        Assert.That(
            sut.CollapsedBreadcrumbs.Count + sut.VisibleBreadcrumbs.Count,
            Is.EqualTo(5));
    }

    [Test]
    public void Breadcrumbs_WhenNarrow_ShowsOnlyCurrentCrumb()
    {
        var sut = CreateSut();
        sut.IsNarrow = true;
        foreach (var t in new[] { "A", "B", "C" })
            sut.Breadcrumbs.Add(new BreadcrumbNode(t, A.Fake<ViewModelBase>()));

        Assert.That(sut.VisibleBreadcrumbs.Select(n => n.Title), Is.EqualTo(new[] { "C" }));
        Assert.That(sut.CollapsedBreadcrumbs.Select(n => n.Title), Is.EqualTo(new[] { "A", "B" }));
    }

    [Test]
    public void Breadcrumbs_TogglingNarrow_RecomputesTrail()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        foreach (var t in new[] { "A", "B" })
            sut.Breadcrumbs.Add(new BreadcrumbNode(t, A.Fake<ViewModelBase>()));

        Assert.That(sut.HasCollapsedBreadcrumbs, Is.False);

        sut.IsNarrow = true;

        Assert.That(sut.HasCollapsedBreadcrumbs, Is.True);
        Assert.That(sut.VisibleBreadcrumbs.Select(n => n.Title), Is.EqualTo(new[] { "B" }));
    }

    [Test]
    public void BreadcrumbMaxWidth_IsSmallerWhenNarrow()
    {
        var sut = CreateSut();

        sut.IsNarrow = false;
        var wide = sut.BreadcrumbMaxWidth;

        sut.IsNarrow = true;
        var narrow = sut.BreadcrumbMaxWidth;

        Assert.That(wide, Is.GreaterThan(narrow), "wide mode allows wider crumbs than narrow mode");
        Assert.That(narrow, Is.GreaterThan(0), "narrow mode caps crumb width to a finite value so it trims with an ellipsis");
        Assert.That(wide, Is.LessThan(double.PositiveInfinity), "wide mode still caps crumb width so a single long name can't overrun the bar");
    }

    [Test]
    public void BreadcrumbMaxWidth_TogglingNarrow_RaisesPropertyChanged()
    {
        var sut = CreateSut();
        sut.IsNarrow = false;
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.IsNarrow = true;

        Assert.That(raised, Does.Contain(nameof(MainWindowViewModel.BreadcrumbMaxWidth)));
    }

    [Test]
    public void Breadcrumbs_AddingNode_RaisesTrailPropertyChanged()
    {
        var sut = CreateSut();
        var raised = new List<string>();
        sut.PropertyChanged += (_, e) => { if (e.PropertyName is not null) raised.Add(e.PropertyName); };

        sut.Breadcrumbs.Add(new BreadcrumbNode("A", A.Fake<ViewModelBase>()));

        Assert.That(raised, Does.Contain(nameof(MainWindowViewModel.VisibleBreadcrumbs)));
        Assert.That(raised, Does.Contain(nameof(MainWindowViewModel.HasCollapsedBreadcrumbs)));
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
    public async Task NavigateToSystemFieldLibrary_WhenNarrow_ClosesSidebar()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = true;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnNavigateToSystemFields?.Invoke();

        Assert.That(sut.IsSidebarOpen, Is.False);
    }

    [Test]
    public async Task NavigateToSystemFieldLibrary_WhenWide_LeavesSidebarOpen()
    {
        var sut = CreateSut();
        await sut.InitializeAsync();
        sut.IsNarrow = false;
        sut.IsSidebarOpen = true;

        sut.SidebarViewModel!.OnNavigateToSystemFields?.Invoke();

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
    public async Task StartAsync_WhenRequireLogin_ShowsLoginAndIsNotAuthenticated()
    {
        var sut = CreateSut();

        await sut.StartAsync(requireLogin: true);

        Assert.Multiple(() =>
        {
            Assert.That(sut.Login, Is.Not.Null);
            Assert.That(sut.IsAuthenticated, Is.False);
            Assert.That(sut.CanLogout, Is.True);
            Assert.That(sut.SidebarViewModel, Is.Null);
        });
    }

    [Test]
    public async Task StartAsync_WhenNoLogin_InitializesAndIsAuthenticated()
    {
        var sut = CreateSut();

        await sut.StartAsync(requireLogin: false);

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.True);
            Assert.That(sut.CanLogout, Is.False);
            Assert.That(sut.Login, Is.Null);
            Assert.That(sut.SidebarViewModel, Is.Not.Null);
        });
    }

    [Test]
    public async Task OnAuthenticated_AfterLoginSucceeds_SetsAuthenticatedAndInitializes()
    {
        A.CallTo(() => _authService.LoginAsync("alice", "pw")).Returns(new User { Username = "alice" });
        var sut = CreateSut();
        await sut.StartAsync(requireLogin: true);
        sut.Login!.Username = "alice";
        sut.Login!.Password = "pw";

        await sut.Login!.SubmitCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.True);
            Assert.That(sut.Login, Is.Null);
            Assert.That(sut.SidebarViewModel, Is.Not.Null);
        });
    }

    private PresetEditorViewModel CreateEditor() => new(
        _presetUseCase,
        _systemFieldUseCase,
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

    [Test]
    public async Task Logout_ClearsSessionAndReturnsToLogin()
    {
        A.CallTo(() => _authService.LoginAsync("alice", "pw")).Returns(new User { Username = "alice" });
        var sut = CreateSut();
        await sut.StartAsync(requireLogin: true);
        sut.Login!.Username = "alice";
        sut.Login!.Password = "pw";
        await sut.Login!.SubmitCommand.ExecuteAsync(null);

        sut.LogoutCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(sut.IsAuthenticated, Is.False);
            Assert.That(sut.Login, Is.Not.Null);
            Assert.That(sut.Breadcrumbs, Is.Empty);
        });
        A.CallTo(() => _authService.Logout()).MustHaveHappenedOnceExactly();
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
}
