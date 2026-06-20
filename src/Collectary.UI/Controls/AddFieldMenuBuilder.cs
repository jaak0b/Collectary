using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls;
using Collectary.Core.Domain.Fields;
using Collectary.Presentation.Localization;
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
            if (previous is null || previous != entry.Category)
                items.Add(CategoryHeader(entry.Category));
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

    private MenuItem CategoryHeader(FieldCategory category) => new()
    {
        Header = category.ToLocalizedString(),
        IsEnabled = false,
        Classes = { "category-header" },
    };
}
