using Avalonia.Media;
using Collectary.Core.Domain;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ProfileTileViewModelTest
{
    [Test]
    public void Name_ReturnsDisplayName()
    {
        var tile = new ProfileTileViewModel(new User { DisplayName = "Alice" });

        Assert.That(tile.Name, Is.EqualTo("Alice"));
    }

    [Test]
    public void Initial_IsFirstLetterUpper()
    {
        var tile = new ProfileTileViewModel(new User { DisplayName = "alice" });

        Assert.That(tile.Initial, Is.EqualTo("A"));
    }

    [Test]
    public void Initial_WhenNameBlank_ReturnsPlaceholder()
    {
        var tile = new ProfileTileViewModel(new User { DisplayName = "   " });

        Assert.That(tile.Initial, Is.EqualTo("?"));
    }

    [Test]
    public void AvatarBrush_IsDeterministicForSameName()
    {
        var first = ((ISolidColorBrush)new ProfileTileViewModel(new User { DisplayName = "Alice" }).AvatarBrush).Color;
        var second = ((ISolidColorBrush)new ProfileTileViewModel(new User { DisplayName = "Alice" }).AvatarBrush).Color;

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void AvatarBrush_DiffersForDifferentNames()
    {
        var alice = ((ISolidColorBrush)new ProfileTileViewModel(new User { DisplayName = "Alice" }).AvatarBrush).Color;
        var bob = ((ISolidColorBrush)new ProfileTileViewModel(new User { DisplayName = "Bob" }).AvatarBrush).Color;

        Assert.That(alice, Is.Not.EqualTo(bob));
    }
}
