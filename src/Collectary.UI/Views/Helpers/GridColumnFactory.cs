using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;

namespace Collectary.UI.Views.Helpers;

public static class GridColumnFactory
{
    public static void AttachRowContextMenu<TRow>(DataGrid grid, IReadOnlyList<(string Header, Action<TRow> Run)> actions) where TRow : class
    {
        grid.LoadingRow -= OnLoadingRow;
        grid.LoadingRow += OnLoadingRow;

        void OnLoadingRow(object? sender, DataGridRowEventArgs args)
        {
            if (args.Row.DataContext is not TRow row) return;
            var menu = new ContextMenu();
            foreach (var action in actions)
            {
                var menuItem = new MenuItem { Header = action.Header };
                var captured = action;
                menuItem.Click += (_, _) => captured.Run(row);
                menu.Items.Add(menuItem);
            }
            args.Row.ContextMenu = menu;
        }
    }

    public static DataGridColumn ValueColumn<TRow>(string header, int cellIndex) where TRow : class =>
        new DataGridTemplateColumn
        {
            Header = header,
            Width = DataGridLength.Auto,
            CellTemplate = new FuncDataTemplate<TRow>((_, _) =>
            {
                var content = new ContentControl();
                content.Bind(ContentControl.ContentProperty, new Binding($"ListCells[{cellIndex}]"));
                return content;
            })
        };

    public static DataGridColumn ActionColumn<TRow>(IReadOnlyList<(string Header, Action<TRow> Run)> actions) where TRow : class =>
        new DataGridTemplateColumn
        {
            Header = "",
            Width = DataGridLength.Auto,
            CellTemplate = new FuncDataTemplate<TRow>((row, _) =>
            {
                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(4, 2)
                };

                var flyout = new MenuFlyout();
                foreach (var action in actions)
                {
                    var menuItem = new MenuItem { Header = action.Header };
                    var captured = action;
                    menuItem.Click += (_, _) =>
                    {
                        if (row is not null) captured.Run(row);
                    };
                    flyout.Items.Add(menuItem);
                }

                var menuButton = new Button { Content = "⋯" };
                FlyoutBase.SetAttachedFlyout(menuButton, flyout);
                menuButton.Click += (_, _) => FlyoutBase.ShowAttachedFlyout(menuButton);

                panel.Children.Add(menuButton);
                return panel;
            })
        };
}
