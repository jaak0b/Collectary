using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

public class AddFieldMenuBuilder
{
    public List<Control> BuildCatalogItems(IReadOnlyList<FieldTypeCatalogEntry> entries, ICommand addCommand)
    {
        var items = new List<Control>();
        FieldCategory? previous = null;
        foreach (var entry in entries)
        {
            if (previous is { } p && p != entry.Category)
                items.Add(new Separator());
            items.Add(new MenuItem
            {
                Icon = new TextBlock { Text = entry.Icon, Classes = { "icon" } },
                Header = entry.Name,
                Command = addCommand,
                CommandParameter = entry,
            });
            previous = entry.Category;
        }
        return items;
    }
}
