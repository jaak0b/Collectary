using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Controls;

/// <summary>
/// Builds the "Add field" menu items from the shared <see cref="FieldTypeCatalog"/>, inserting a
/// <see cref="Separator"/> between field categories. Used by both the preset editor and the system-field
/// library so their menus stay identical (Avalonia 12 renders dynamic menus reliably only from code-behind).
/// </summary>
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
                Header = $"{entry.Icon}  {entry.Name}",
                Command = addCommand,
                CommandParameter = entry,
            });
            previous = entry.Category;
        }
        return items;
    }
}
