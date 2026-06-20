using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views;

public partial class TagsFieldEditorView : UserControl
{
    private TextBox? _entry;
    private TagsFieldEditorViewModel? _vm;

    public TagsFieldEditorView()
    {
        InitializeComponent();
        FieldBorder.PointerPressed += (_, _) => _entry?.Focus();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_vm is not null)
            _vm.Tags.CollectionChanged -= OnTagsChanged;

        _vm = DataContext as TagsFieldEditorViewModel;

        if (_vm is not null)
            _vm.Tags.CollectionChanged += OnTagsChanged;

        RebuildChips();
    }

    private void OnTagsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildChips();

    private void RebuildChips()
    {
        var entry = GetOrCreateEntry();

        for (var i = TagPanel.Children.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(TagPanel.Children[i], entry))
                TagPanel.Children.RemoveAt(i);
        }

        if (_vm is null)
        {
            TagPanel.Children.Remove(entry);
            return;
        }

        if (!TagPanel.Children.Contains(entry))
            TagPanel.Children.Add(entry);

        var index = 0;
        foreach (var tag in _vm.Tags)
            TagPanel.Children.Insert(index++, BuildChip(tag));
    }

    private Border BuildChip(string tag)
    {
        var label = new TextBlock
        {
            Text = tag,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = 220,
        };
        label.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("PrimaryForegroundBrush"));

        var remove = new TextBlock
        {
            Text = IconGlyphs.Dismiss,
            FontSize = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
        };
        remove.Classes.Add("icon");
        remove.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("PrimaryForegroundBrush"));
        remove.PointerPressed += (_, _) => _vm?.RemoveTagCommand.Execute(tag);

        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        content.Children.Add(label);
        content.Children.Add(remove);

        var chip = new Border
        {
            CornerRadius = new CornerRadius(6),
            Margin = new Thickness(0, 2, 4, 2),
            Padding = new Thickness(6),
            Child = content,
        };
        chip.Bind(Border.BackgroundProperty, this.GetResourceObservable("PrimaryBrush"));
        return chip;
    }

    private TextBox GetOrCreateEntry()
    {
        if (_entry is not null) return _entry;

        _entry = new TextBox
        {
            MinWidth = 80,
            Margin = new Thickness(0, 2, 0, 2),
            VerticalAlignment = VerticalAlignment.Center,
        };
        _entry.Classes.Add("tag-entry");
        _entry.Bind(TextBox.TextProperty, new Binding(nameof(TagsFieldEditorViewModel.NewTag)) { Mode = BindingMode.TwoWay });
        _entry.Bind(TextBox.PlaceholderTextProperty, new Binding("[Tags_AddPlaceholder]") { Source = LocalizationService.Instance });
        _entry.AddHandler(InputElement.KeyDownEvent, OnEntryKeyDown, RoutingStrategies.Tunnel);
        _entry.GotFocus += (_, _) => FieldBorder.Bind(Border.BorderBrushProperty, this.GetResourceObservable("FocusRingBrush"));
        _entry.LostFocus += (_, _) => FieldBorder.Bind(Border.BorderBrushProperty, this.GetResourceObservable("BorderBrush"));
        return _entry;
    }

    private void OnEntryKeyDown(object? sender, KeyEventArgs e)
    {
        if (_vm is null) return;

        if (e.Key == Key.Enter)
        {
            _vm.AddTagCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Back && string.IsNullOrEmpty(_vm.NewTag))
        {
            _vm.RemoveLastTagCommand.Execute(null);
            e.Handled = true;
        }
    }
}
