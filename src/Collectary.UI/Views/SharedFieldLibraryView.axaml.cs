using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using CommunityToolkit.Mvvm.Messaging;
using Collectary.Core.Domain.Fields;
using Collectary.UI.Controls;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.UI.Views.SharedFields;

public partial class SharedFieldLibraryView : UserControl
{
    private readonly ResponsiveSplitLayout _layout;
    private readonly PointerReorderBehavior _reorder;
    private readonly AddFieldMenuBuilder _menuBuilder = new();

    public SharedFieldLibraryView()
    {
        InitializeComponent();
        _layout = new ResponsiveSplitLayout(SplitGrid, MasterPane, PaneSplitter, DetailPane);
        _reorder = new PointerReorderBehavior(FieldListBox,
            (from, to) => (DataContext as SharedFieldLibraryViewModel)?.MoveField(from, to),
            () => _ = (DataContext as SharedFieldLibraryViewModel)?.CommitReorderAsync(),
            OnDragActive);
        DataContextChanged += OnDataContextChanged;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, static (recipient, _) =>
            ((SharedFieldLibraryView)recipient).BuildAddFieldMenu());
    }

    private void OnDataContextChanged(object? sender, EventArgs e) => BuildAddFieldMenu();

    private void BuildAddFieldMenu()
    {
        if (DataContext is not SharedFieldLibraryViewModel vm) return;

        var items = _menuBuilder.BuildCatalogItems(vm.AddableFieldTypes, vm.AddFieldOfTypeCommand);

        var addGroup = new MenuItem
        {
            Icon = new TextBlock { Text = IconGlyphs.Folder, Classes = { "icon" } },
            Header = LocalizationService.Instance["AddGroup"],
            Command = vm.AddGroupCommand,
        };
        addGroup.Bind(MenuItem.IsEnabledProperty, new Binding
        {
            Source = vm,
            Path = nameof(SharedFieldLibraryViewModel.CurrentLevelSupportsGroups),
        });
        items.Add(addGroup);

        ((MenuFlyout)AddFieldButton.Flyout!).ItemsSource = items;
    }

    private void OnDragActive(object? item, bool active)
    {
        if (item is IDraggableRow row) row.IsDragging = active;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _reorder.Attach();
        _layout.Attach(Bounds.Width);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _reorder.Detach();
        _layout.Detach();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        _layout.Apply(e.NewSize.Width);
        if (DataContext is FieldListEditorViewModel vm)
            vm.IsNarrow = e.NewSize.Width < ResponsiveSplitLayout.NarrowThreshold;
    }
}
