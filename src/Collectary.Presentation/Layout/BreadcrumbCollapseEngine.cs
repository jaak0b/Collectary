namespace Collectary.Presentation.Layout;

public record BreadcrumbCollapse(
    IReadOnlyList<int> VisibleIndices,
    IReadOnlyList<int> CollapsedIndices,
    bool ShowOverflow,
    bool MustTrimCurrent);

public class BreadcrumbCollapseEngine
{
    public BreadcrumbCollapse Resolve(
        IReadOnlyList<double> itemWidths,
        double availableWidth,
        double overflowWidth,
        int homeIndex,
        int currentIndex)
    {
        int count = itemWidths.Count;
        if (count == 0)
            return new BreadcrumbCollapse(Array.Empty<int>(), Array.Empty<int>(), false, false);

        if (itemWidths.Sum() <= availableWidth)
            return new BreadcrumbCollapse(Enumerable.Range(0, count).ToList(), Array.Empty<int>(), false, false);

        double currentWidth = itemWidths[currentIndex];
        double homeWidth = itemWidths[homeIndex];

        double remaining = availableWidth - currentWidth - overflowWidth;
        bool keepHome = homeIndex != currentIndex && homeWidth <= remaining;
        if (keepHome) remaining -= homeWidth;

        int lowestTail = currentIndex;
        for (int i = currentIndex - 1; i > homeIndex; i--)
        {
            if (itemWidths[i] > remaining) break;
            remaining -= itemWidths[i];
            lowestTail = i;
        }

        var visible = new List<int>();
        if (keepHome) visible.Add(homeIndex);
        visible.AddRange(Enumerable.Range(lowestTail, currentIndex - lowestTail + 1));

        var visibleSet = new HashSet<int>(visible);
        var collapsed = Enumerable.Range(0, count).Where(i => !visibleSet.Contains(i)).ToList();

        bool showOverflow = collapsed.Count > 0;
        double visibleWidth = visible.Sum(i => itemWidths[i]) + (showOverflow ? overflowWidth : 0);
        bool mustTrim = visibleWidth > availableWidth;
        return new BreadcrumbCollapse(visible, collapsed, showOverflow, mustTrim);
    }
}
