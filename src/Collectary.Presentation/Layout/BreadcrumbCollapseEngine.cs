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

        double homeWidth = itemWidths[homeIndex];
        double currentWidth = itemWidths[currentIndex];
        var allVisible = Enumerable.Range(0, count).ToList();

        if (currentIndex - homeIndex < 2)
            return new BreadcrumbCollapse(allVisible, Array.Empty<int>(), false, homeWidth + currentWidth > availableWidth);

        double suffixBudget = availableWidth - homeWidth - overflowWidth;
        int lowestKept = currentIndex;
        double running = 0;
        for (int i = currentIndex; i > homeIndex; i--)
        {
            if (i != currentIndex && running + itemWidths[i] > suffixBudget) break;
            running += itemWidths[i];
            lowestKept = i;
        }

        var visible = new List<int> { homeIndex };
        visible.AddRange(Enumerable.Range(lowestKept, currentIndex - lowestKept + 1));
        var collapsed = Enumerable.Range(homeIndex + 1, lowestKept - homeIndex - 1).ToList();

        bool mustTrim = homeWidth + overflowWidth + currentWidth > availableWidth;
        return new BreadcrumbCollapse(visible, collapsed, true, mustTrim);
    }
}
