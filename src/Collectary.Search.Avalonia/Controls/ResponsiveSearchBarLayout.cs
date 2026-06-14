namespace Collectary.Search.Avalonia.Controls;

public sealed class ResponsiveSearchBarLayout
{
    private readonly double _spacing;

    public ResponsiveSearchBarLayout(double spacing = 24) => _spacing = spacing;

    public bool ShouldStack(double availableWidth, double naturalRowWidth) =>
        availableWidth > 0 && naturalRowWidth + _spacing > availableWidth;
}
