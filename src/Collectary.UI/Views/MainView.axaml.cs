using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Collectary.UI.Controls;
using Collectary.UI.Views.Helpers;
using Collectary.Presentation.Localization;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class MainView : UserControl
{
    private const double MinSidebarWidth = 180;
    private const double MaxSidebarWidth = 480;

    private readonly BreadcrumbVisualBuilder _breadcrumbBuilder = new();
    private MainWindowViewModel? _vm;
    private Button? _overflowButton;
    private Flyout? _overflowFlyout;
    private TextBlock? _overflowSeparator;
    private Flyout? _syncFlyout;
    private TextBlock? _syncLastText;
    private TextBlock? _syncStatusText;
    private TextBlock? _syncNoticeText;

    public MainView()
    {
        InitializeComponent();
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private ColumnDefinition SidebarColumn => BodyGrid.ColumnDefinitions[0];

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        _vm = vm;
        _vm.PropertyChanged += OnVmPropertyChanged;
        _vm.BreadcrumbItems.CollectionChanged += OnBreadcrumbItemsChanged;
        BreadcrumbBar.CollapsedChanged += OnBreadcrumbCollapsedChanged;
        SidebarSplitter.AddHandler(PointerReleasedEvent, OnSplitterReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        _syncFlyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedRight, Content = BuildSyncFlyoutContent(_vm.Sync) };
        SyncStatusButton.Flyout = _syncFlyout;
        _vm.Sync.PropertyChanged += OnSyncStateChanged;
        ApplySidebarState();
        RebuildBreadcrumbs();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.BreadcrumbItems.CollectionChanged -= OnBreadcrumbItemsChanged;
            _vm.Sync.PropertyChanged -= OnSyncStateChanged;
        }
        BreadcrumbBar.CollapsedChanged -= OnBreadcrumbCollapsedChanged;
        SidebarSplitter.RemoveHandler(PointerReleasedEvent, OnSplitterReleased);
    }

    private Control BuildSyncFlyoutContent(SyncViewModel sync)
    {
        _syncLastText = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = ThemeBrush("TextSecondaryBrush") };
        _syncStatusText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        _syncNoticeText = new TextBlock { TextWrapping = TextWrapping.Wrap };

        var syncNow = new Button
        {
            Content = LocalizationService.Instance["Sync_Now"],
            Command = sync.SyncNowCommand,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        var panel = new StackPanel { Spacing = 8, MinWidth = 200, MaxWidth = 320, Margin = new Thickness(4) };
        panel.Children.Add(_syncLastText);
        panel.Children.Add(_syncStatusText);
        panel.Children.Add(_syncNoticeText);
        panel.Children.Add(syncNow);
        RefreshSyncFlyout(sync);
        return panel;
    }

    private void OnSyncStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_vm is not null) RefreshSyncFlyout(_vm.Sync);
    }

    private void RefreshSyncFlyout(SyncViewModel sync)
    {
        if (_syncLastText is null || _syncStatusText is null || _syncNoticeText is null) return;

        _syncLastText.Text = sync.LastSyncText;
        _syncStatusText.Text = sync.IsSyncing ? LocalizationService.Instance["Sync_Syncing"] : sync.LastResultText;
        _syncStatusText.IsVisible = !string.IsNullOrEmpty(_syncStatusText.Text);
        _syncNoticeText.Text = sync.ErrorMessage;
        _syncNoticeText.IsVisible = sync.NeedsAttention && !string.IsNullOrEmpty(sync.ErrorMessage);
        _syncNoticeText.Foreground = ThemeBrush(sync.IsError ? "DangerBrush" : "WarningBrush");
    }

    private IBrush? ThemeBrush(string key) =>
        this.TryFindResource(key, out var value) && value is IBrush brush ? brush : null;

    private void OnBreadcrumbItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildBreadcrumbs();

    private void RebuildBreadcrumbs()
    {
        if (_vm is null) return;

        BreadcrumbBar.Children.Clear();

        var items = _vm.BreadcrumbItems;
        if (items.Count == 0) return;

        BreadcrumbBar.Children.Add(_breadcrumbBuilder.BuildCrumb(items[0]));

        _overflowFlyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        _overflowButton = CreateOverflowButton(_overflowFlyout);
        BreadcrumbBar.Children.Add(_overflowButton);

        for (var i = 1; i < items.Count; i++)
            BreadcrumbBar.Children.Add(_breadcrumbBuilder.BuildCrumb(items[i]));
    }

    private Button CreateOverflowButton(Flyout flyout)
    {
        _overflowSeparator = _breadcrumbBuilder.BuildSeparator();
        var glyph = new TextBlock
        {
            Text = "…",
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };

        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(_overflowSeparator);
        content.Children.Add(glyph);

        var button = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Content = content,
            Flyout = flyout
        };
        button.Click += (_, _) => PopulateOverflowFlyout();
        BreadcrumbBarPanel.SetIsOverflow(button, true);
        return button;
    }

    private void PopulateOverflowFlyout()
    {
        if (_vm is null || _overflowFlyout is null) return;

        var panel = new StackPanel();
        foreach (var index in BreadcrumbBar.CollapsedIndices)
        {
            if (index < 0 || index >= _vm.BreadcrumbItems.Count) continue;
            var item = _vm.BreadcrumbItems[index];
            panel.Children.Add(new Button
            {
                Content = item.Title,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 4),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontSize = 14,
                Command = item.NavigateCommand,
                CommandParameter = item.CommandParameter
            });
        }
        _overflowFlyout.Content = panel;
    }

    private void OnBreadcrumbCollapsedChanged(object? sender, EventArgs e)
    {
        if (_overflowSeparator is not null)
            _overflowSeparator.Opacity = BreadcrumbBar.CollapsedIndices.Contains(0) ? 0 : 1;
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsSidebarOpen))
            ApplySidebarState();
    }

    private void ApplySidebarState()
    {
        if (_vm is null) return;
        var column = SidebarColumn;
        if (_vm.IsSidebarOpen)
        {
            column.MinWidth = MinSidebarWidth;
            column.MaxWidth = MaxSidebarWidth;
            var width = Math.Clamp(_vm.SidebarWidth, MinSidebarWidth, MaxSidebarWidth);
            column.Width = new GridLength(width, GridUnitType.Pixel);
        }
        else
        {
            column.MinWidth = 0;
            column.MaxWidth = 0;
            column.Width = new GridLength(0, GridUnitType.Pixel);
        }
    }

    private void OnSplitterReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_vm is null || !_vm.IsSidebarOpen) return;
        var width = SidebarColumn.Width;
        if (!width.IsAbsolute || width.Value <= 0) return;
        _vm.SidebarWidth = width.Value;
        AppPreferences.Update(p => p with { SidebarWidth = width.Value });
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;
        vm.IsNarrow = e.NewSize.Width < ResponsiveSplitLayout.NarrowThreshold;
    }
}
