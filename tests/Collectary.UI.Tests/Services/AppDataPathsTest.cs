using Collectary.Presentation.Services;

namespace Collectary.UI.Tests.Services;

[TestFixture]
public class AppDataPathsTest
{
    [Test]
    public void Root_IsRootedAndConfigured()
    {
        Assert.That(Path.IsPathRooted(AppDataPaths.Root), Is.True);
#if DEBUG
        // Debug isolates data next to the build output so multiple worktrees/instances don't share
        // (and corrupt) one database.
        Assert.That(AppDataPaths.Root, Does.Contain("collectary-data"));
#else
        Assert.That(AppDataPaths.Root, Does.EndWith("Collectary"));
#endif
    }
}
