using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
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
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, static (recipient, _) =>
            ((PresetEditorView)recipient).BuildAddFieldMenu());
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
        var narrow = e.NewSize.Width < ResponsiveSplitLayout.NarrowThreshold;
        _layout.Apply(e.NewSize.Width);
        ApplyHeaderLayout(narrow);
        if (DataContext is FieldListEditorViewModel vm)
            vm.IsNarrow = narrow;
    }

    private bool? _headerNarrow;

    private void ApplyHeaderLayout(bool narrow)
    {
        if (_headerNarrow == narrow) return;
        _headerNarrow = narrow;

        LabelLayoutField.IsVisible = !narrow;

        if (narrow)
        {
            CollectionSettingsHeader.ColumnDefinitions = new ColumnDefinitions("Auto,*");
            CollectionSettingsHeader.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
            Place(NameGroup, row: 0, column: 0, columnSpan: 2);
            Place(ColumnStepper, row: 1, column: 0, columnSpan: 1);
            Place(ParentField, row: 1, column: 1, columnSpan: 1);
            Place(NameWarningText, row: 2, column: 0, columnSpan: 2);
        }
        else
        {
            CollectionSettingsHeader.ColumnDefinitions = new ColumnDefinitions("*,*,Auto,Auto");
            CollectionSettingsHeader.RowDefinitions = new RowDefinitions("Auto,Auto");
            Place(NameGroup, row: 0, column: 0, columnSpan: 1);
            Place(ParentField, row: 0, column: 1, columnSpan: 1);
            Place(ColumnStepper, row: 0, column: 2, columnSpan: 1);
            Place(LabelLayoutField, row: 0, column: 3, columnSpan: 1);
            Place(NameWarningText, row: 1, column: 0, columnSpan: 4);
        }
    }

    private void Place(Control control, int row, int column, int columnSpan)
    {
        Grid.SetRow(control, row);
        Grid.SetColumn(control, column);
        Grid.SetColumnSpan(control, columnSpan);
    }
}
