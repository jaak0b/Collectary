using System;
using System.Collections.Generic;
using System.Linq;

namespace Collectary.Presentation.ViewModels;

public class BreadcrumbTrail<T>
{
    public IReadOnlyList<T> Collapsed { get; }
    public IReadOnlyList<T> Visible { get; }
    public bool HasCollapsed => Collapsed.Count > 0;

    public BreadcrumbTrail(IReadOnlyList<T> nodes, int maxVisible)
    {
        var keep = Math.Max(1, maxVisible);
        if (nodes.Count <= keep)
        {
            Collapsed = Array.Empty<T>();
            Visible = nodes.ToList();
            return;
        }

        Collapsed = nodes.Take(nodes.Count - keep).ToList();
        Visible = nodes.Skip(nodes.Count - keep).ToList();
    }
}
