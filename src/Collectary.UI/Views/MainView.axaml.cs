using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Collectary.UI.Controls;
using Collectary.UI.Services;
using Collectary.UI.ViewModels;

namespace Collectary.UI.Views;

public partial class MainView : UserControl
{
    private const double MinSidebarWidth = 180;
    private const double MaxSidebarWidth = 480;

    private MainWindowViewModel? _vm;

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
        SidebarSplitter.AddHandler(PointerReleasedEvent, OnSplitterReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        ApplySidebarState();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;
        SidebarSplitter.RemoveHandler(PointerReleasedEvent, OnSplitterReleased);
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
        var prefs = AppPreferences.Load();
        AppPreferences.Save(prefs with { SidebarWidth = width.Value });
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
