using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
using Collectary.Presentation.ViewModels;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SyncConflictViewModelTest
{
    private static SyncConflict Conflict(SyncEntityKind kind) =>
        new(kind, Guid.NewGuid(), "Mine", "Theirs", 3, 5);

    [Test]
    public void Labels_ExposeConflictLabels()
    {
        var vm = new SyncConflictViewModel(Conflict(SyncEntityKind.Item), (_, _) => Task.CompletedTask);

        Assert.Multiple(() =>
        {
            Assert.That(vm.LocalLabel, Is.EqualTo("Mine"));
            Assert.That(vm.RemoteLabel, Is.EqualTo("Theirs"));
        });
    }

    [TestCase(SyncEntityKind.Preset, "Sync_KindPreset")]
    [TestCase(SyncEntityKind.Item, "Sync_KindItem")]
    [TestCase(SyncEntityKind.SharedField, "Sync_KindSharedField")]
    [TestCase(SyncEntityKind.User, "Sync_KindUser")]
    [TestCase(SyncEntityKind.Share, "Sync_KindShare")]
    public void KindText_IsLocalizedPerKind(SyncEntityKind kind, string key)
    {
        var vm = new SyncConflictViewModel(Conflict(kind), (_, _) => Task.CompletedTask);

        Assert.That(vm.KindText, Is.EqualTo(LocalizationService.Instance[key]));
    }

    [Test]
    public void KindText_IsDistinctForEveryKind_SoUserAndShareAreNotMislabeled()
    {
        var labels = Enum.GetValues<SyncEntityKind>()
            .Select(k => new SyncConflictViewModel(Conflict(k), (_, _) => Task.CompletedTask).KindText)
            .ToList();

        Assert.That(labels, Is.Unique,
            "each kind must render its own label — User/Share must not fall through to the shared-field label");
    }

    [Test]
    public async Task KeepMine_ResolvesKeepingLocal()
    {
        var conflict = Conflict(SyncEntityKind.Item);
        SyncConflict? seen = null;
        bool? keepLocal = null;
        var vm = new SyncConflictViewModel(conflict, (c, keep) => { seen = c; keepLocal = keep; return Task.CompletedTask; });

        await vm.KeepMineCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(seen, Is.EqualTo(conflict));
            Assert.That(keepLocal, Is.True);
        });
    }

    [Test]
    public async Task KeepTheirs_ResolvesTakingRemote()
    {
        var conflict = Conflict(SyncEntityKind.Item);
        bool? keepLocal = null;
        var vm = new SyncConflictViewModel(conflict, (_, keep) => { keepLocal = keep; return Task.CompletedTask; });

        await vm.KeepTheirsCommand.ExecuteAsync(null);

        Assert.That(keepLocal, Is.False);
    }
}
