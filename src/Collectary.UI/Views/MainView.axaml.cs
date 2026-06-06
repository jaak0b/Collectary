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
using Collectary.Core.Domain.Fields;
using Collectary.UI.Controls;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class MainView : UserControl
{
    private const double MinSidebarWidth = 180;
    private const double MaxSidebarWidth = 480;

    private MainWindowViewModel? _vm;
    private Button? _overflowButton;
    private Flyout? _overflowFlyout;
    private IReadOnlyList<int> _appliedCollapsed = Array.Empty<int>();

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
        BreadcrumbBar.LayoutUpdated += OnBreadcrumbBarLayoutUpdated;
        SidebarSplitter.AddHandler(PointerReleasedEvent, OnSplitterReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        ApplySidebarState();
        RebuildBreadcrumbs();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
            _vm.BreadcrumbItems.CollectionChanged -= OnBreadcrumbItemsChanged;
        }
        BreadcrumbBar.LayoutUpdated -= OnBreadcrumbBarLayoutUpdated;
        SidebarSplitter.RemoveHandler(PointerReleasedEvent, OnSplitterReleased);
    }

    private void OnBreadcrumbItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildBreadcrumbs();

    private void RebuildBreadcrumbs()
    {
        if (_vm is null) return;

        BreadcrumbBar.Children.Clear();
        _appliedCollapsed = Array.Empty<int>();

        var items = _vm.BreadcrumbItems;
        if (items.Count == 0) return;

        BreadcrumbBar.Children.Add(CreateCrumbButton(items[0]));

        _overflowFlyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
        _overflowButton = CreateOverflowButton(_overflowFlyout);
        BreadcrumbBar.Children.Add(_overflowButton);

        for (var i = 1; i < items.Count; i++)
            BreadcrumbBar.Children.Add(CreateCrumbButton(items[i]));
    }

    private Button CreateCrumbButton(BreadcrumbItem item)
    {
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            FontWeight = item.IsCurrent ? FontWeight.SemiBold : item.IsHome ? FontWeight.Medium : FontWeight.Normal
        };

        object content;
        if (item.IsHome)
        {
            content = title;
        }
        else
        {
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
            var separator = new TextBlock
            {
                Text = "/",
                Margin = new Thickness(2, 0),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(separator, 0);
            Grid.SetColumn(title, 1);
            grid.Children.Add(separator);
            grid.Children.Add(title);
            content = grid;
        }

        return new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(item.IsHome ? 8 : 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Content = content,
            Command = item.NavigateCommand,
            CommandParameter = item.CommandParameter
        };
    }

    private Button CreateOverflowButton(Flyout flyout)
    {
        var separator = new TextBlock
        {
            Text = "/",
            Margin = new Thickness(2, 0),
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        var glyph = new TextBlock
        {
            Text = IconGlyphs.MoreHorizontal,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
        glyph.Classes.Add("icon");

        var content = new StackPanel { Orientation = Orientation.Horizontal };
        content.Children.Add(separator);
        content.Children.Add(glyph);

        var button = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Content = content,
            Flyout = flyout
        };
        BreadcrumbBarPanel.SetIsOverflow(button, true);
        return button;
    }

    private void OnBreadcrumbBarLayoutUpdated(object? sender, EventArgs e)
    {
        if (_vm is null || _overflowFlyout is null) return;

        var collapsed = BreadcrumbBar.CollapsedIndices;
        if (collapsed.SequenceEqual(_appliedCollapsed)) return;
        _appliedCollapsed = collapsed.ToArray();

        var panel = new StackPanel();
        foreach (var index in collapsed)
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

        var wasNarrow = vm.IsNarrow;
        vm.IsNarrow = e.NewSize.Width < ResponsiveSplitLayout.NarrowThreshold;

        if (wasNarrow && !vm.IsNarrow)
        {
            var prefs = AppPreferences.Load();
            vm.IsSidebarOpen = prefs.SidebarOpen;
        }
    }
}
