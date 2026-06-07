using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Collectary.UI.Controls;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SharedFields;

namespace Collectary.UI.Views;

public partial class PresetEditorView : UserControl
{
    private readonly ResponsiveSplitLayout _layout;
    private readonly PointerReorderBehavior _reorder;
    private readonly AddFieldMenuBuilder _menuBuilder = new();

    private ObservableCollection<SharedFieldRowViewModel>? _sharedFields;

    public PresetEditorView()
    {
        InitializeComponent();
        _layout = new ResponsiveSplitLayout(SplitGrid, MasterPane, PaneSplitter, DetailPane);
        _reorder = new PointerReorderBehavior(FieldListBox,
            (from, to) => (DataContext as PresetEditorViewModel)?.MoveField(from, to),
            () => { },
            OnDragActive);
        DataContextChanged += OnDataContextChanged;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_sharedFields is not null)
            _sharedFields.CollectionChanged -= OnSharedFieldsChanged;

        _sharedFields = (DataContext as PresetEditorViewModel)?.AvailableSharedFields;

        if (_sharedFields is not null)
            _sharedFields.CollectionChanged += OnSharedFieldsChanged;

        BuildAddFieldMenu();
    }

    private void OnSharedFieldsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        BuildAddFieldMenu();

    private void OnLanguageChanged(object? sender, EventArgs e) => BuildAddFieldMenu();

    private void BuildAddFieldMenu()
    {
        if (DataContext is not PresetEditorViewModel vm) return;

        var items = _menuBuilder.BuildCatalogItems(vm.AddableFieldTypes, vm.AddFieldOfTypeCommand);

        items.Add(new Separator());
        items.Add(new MenuItem
        {
            Header = LocalizationService.Instance["AddGroup"],
            Command = vm.AddGroupCommand,
        });

        items.Add(new Separator());
        items.Add(new MenuItem
        {
            Header = LocalizationService.Instance["SharedFields"],
            ItemsSource = BuildSharedFieldItems(),
        });

        ((MenuFlyout)AddFieldButton.Flyout!).ItemsSource = items;
    }

    private List<MenuItem> BuildSharedFieldItems()
    {
        var items = new List<MenuItem>();
        if (_sharedFields is not null)
            foreach (var row in _sharedFields)
                items.Add(new MenuItem
                {
                    Header = row.Name,
                    Command = row.AddToCollectionCommand,
                    CommandParameter = row,
                });
        return items;
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
