using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Collectary.UI.Controls;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;
using Collectary.Presentation.ViewModels.SystemFields;

namespace Collectary.UI.Views;

public partial class PresetEditorView : UserControl
{
    private readonly ResponsiveSplitLayout _layout;
    private readonly ListReorderBehavior _reorder;
    private readonly AddFieldMenuBuilder _menuBuilder = new();

    private ObservableCollection<SystemFieldRowViewModel>? _systemFields;

    public PresetEditorView()
    {
        InitializeComponent();
        _layout = new ResponsiveSplitLayout(SplitGrid, MasterPane, PaneSplitter, DetailPane);
        _reorder = new ListReorderBehavior(FieldListBox,
            (from, to) => (DataContext as PresetEditorViewModel)?.MoveField(from, to));
        DataContextChanged += OnDataContextChanged;
        LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_systemFields is not null)
            _systemFields.CollectionChanged -= OnSystemFieldsChanged;

        _systemFields = (DataContext as PresetEditorViewModel)?.AvailableSystemFields;

        if (_systemFields is not null)
            _systemFields.CollectionChanged += OnSystemFieldsChanged;

        BuildAddFieldMenu();
    }

    private void OnSystemFieldsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
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
            Header = LocalizationService.Instance["SystemFields"],
            ItemsSource = BuildSystemFieldItems(),
        });

        ((MenuFlyout)AddFieldButton.Flyout!).ItemsSource = items;
    }

    private List<MenuItem> BuildSystemFieldItems()
    {
        var items = new List<MenuItem>();
        if (_systemFields is not null)
            foreach (var row in _systemFields)
                items.Add(new MenuItem
                {
                    Header = row.Name,
                    Command = row.AddToCollectionCommand,
                    CommandParameter = row,
                });
        return items;
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
