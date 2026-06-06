using Avalonia.Controls;
using Collectary.UI.Controls;

namespace Collectary.UI.Tests.Controls;

[TestFixture]
public class ResponsiveSplitLayoutTest
{
    private static (Grid grid, Control master, Control splitter, Control detail) Build()
    {
        var master = new Border();
        var splitter = new Border();
        var detail = new Border();
        var grid = new Grid();
        grid.Children.Add(master);
        grid.Children.Add(splitter);
        grid.Children.Add(detail);
        return (grid, master, splitter, detail);
    }

    [Test]
    public void Apply_Narrow_PlacesMasterPaneInStarRow()
    {
        var (grid, master, splitter, detail) = Build();
        var sut = new ResponsiveSplitLayout(grid, master, splitter, detail);

        sut.Apply(400);

        var masterRow = grid.RowDefinitions[Grid.GetRow(master)];
        Assert.That(masterRow.Height.GridUnitType, Is.EqualTo(GridUnitType.Star),
            "master pane must sit in a Star row so its list is height-bounded and scrolls in narrow mode");
    }

    [Test]
    public void Apply_Narrow_PlacesDetailPaneInStarRow()
    {
        var (grid, master, splitter, detail) = Build();
        var sut = new ResponsiveSplitLayout(grid, master, splitter, detail);

        sut.Apply(400);

        var detailRow = grid.RowDefinitions[Grid.GetRow(detail)];
        Assert.That(detailRow.Height.GridUnitType, Is.EqualTo(GridUnitType.Star),
            "detail pane must sit in a Star row so its content is height-bounded and scrolls in narrow mode");
    }

    [Test]
    public void Apply_Narrow_HidesSplitter()
    {
        var (grid, master, splitter, detail) = Build();
        var sut = new ResponsiveSplitLayout(grid, master, splitter, detail);

        sut.Apply(400);

        Assert.That(splitter.IsVisible, Is.False);
    }

    [Test]
    public void Apply_Wide_KeepsMasterAndDetailInStarRow()
    {
        var (grid, master, splitter, detail) = Build();
        var sut = new ResponsiveSplitLayout(grid, master, splitter, detail);

        sut.Apply(1200);

        var masterRow = grid.RowDefinitions[Grid.GetRow(master)];
        var detailRow = grid.RowDefinitions[Grid.GetRow(detail)];
        Assert.That(masterRow.Height.GridUnitType, Is.EqualTo(GridUnitType.Star));
        Assert.That(detailRow.Height.GridUnitType, Is.EqualTo(GridUnitType.Star));
        Assert.That(splitter.IsVisible, Is.True);
    }
}
