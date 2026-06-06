using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Views.Helpers;

public class BreadcrumbVisualBuilder
{
    public double MaxCrumbWidth { get; } = 220;

    public Control BuildCrumb(BreadcrumbItem item)
    {
        var title = new TextBlock
        {
            Text = item.Title,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = MaxCrumbWidth,
            FontWeight = item.IsCurrent ? FontWeight.SemiBold : item.IsHome ? FontWeight.Medium : FontWeight.Normal
        };

        var button = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(4, 2),
            MinWidth = 0,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Content = title,
            Command = item.NavigateCommand,
            CommandParameter = item.CommandParameter
        };

        if (item.IsHome)
            return button;

        var separator = BuildSeparator();
        DockPanel.SetDock(separator, Dock.Left);
        var dock = new DockPanel { LastChildFill = true, VerticalAlignment = VerticalAlignment.Center };
        dock.Children.Add(separator);
        dock.Children.Add(button);
        return dock;
    }

    public TextBlock BuildSeparator() => new()
    {
        Text = "/",
        Margin = new Thickness(6, 0),
        FontSize = 14,
        VerticalAlignment = VerticalAlignment.Center
    };
}
