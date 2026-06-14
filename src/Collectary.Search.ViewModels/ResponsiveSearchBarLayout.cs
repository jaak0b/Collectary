namespace Collectary.Search.ViewModels;

public sealed class ResponsiveSearchBarLayout
{
    public bool ShouldStack(double availableWidth, double naturalRowWidth) =>
        availableWidth > 0 && naturalRowWidth > availableWidth;
}
