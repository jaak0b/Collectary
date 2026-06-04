using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class SyncViewModelTest
{
    private ISyncService _sync = null!;
    private ISyncStatus _status = null!;

    [SetUp]
    public void SetUp()
    {
        _sync = A.Fake<ISyncService>();
        _status = A.Fake<ISyncStatus>();
        A.CallTo(() => _status.IsConfigured).Returns(true);
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, Array.Empty<SyncConflict>()));
    }

    private SyncViewModel Make() => new(_sync, _status);

    [Test]
    public async Task SyncNow_WhenConfigured_SyncsAndSetsLastSynced()
    {
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.LastSyncedAt, Is.Not.Null);
        A.CallTo(() => _sync.SyncAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task SyncNow_WhenNotConfigured_DoesNothing()
    {
        A.CallTo(() => _status.IsConfigured).Returns(false);
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.LastSyncedAt, Is.Null);
        A.CallTo(() => _sync.SyncAsync()).MustNotHaveHappened();
    }

    [Test]
    public async Task SyncNow_WithConflicts_PopulatesConflicts()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, new[]
        {
            new SyncConflict(SyncEntityKind.Preset, Guid.NewGuid(), "Mine", "Theirs", 2, 2),
        }));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.HasConflicts, Is.True);
        Assert.That(vm.Conflicts, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SyncNow_WhenSyncThrows_SetsError()
    {
        A.CallTo(() => _sync.SyncAsync()).Throws(new InvalidOperationException("boom"));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.HasError, Is.True);
        Assert.That(vm.IsSyncing, Is.False);
    }

    [Test]
    public void IsConfigured_ReflectsStatus()
    {
        A.CallTo(() => _status.IsConfigured).Returns(false);
        Assert.That(Make().IsConfigured, Is.False);
    }

    [Test]
    public void Refresh_RaisesIsConfiguredChanged()
    {
        var vm = Make();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.Refresh();

        Assert.That(changed, Does.Contain(nameof(vm.IsConfigured)));
    }

    [Test]
    public async Task SyncNow_WhenSucceeds_RaisesSynced()
    {
        var vm = Make();
        var fired = 0;
        vm.Synced += () => fired++;

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(fired, Is.EqualTo(1));
    }

    [Test]
    public async Task SyncNow_WhenSyncThrows_DoesNotRaiseSynced()
    {
        A.CallTo(() => _sync.SyncAsync()).Throws(new InvalidOperationException("boom"));
        var vm = Make();
        var fired = 0;
        vm.Synced += () => fired++;

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(fired, Is.EqualTo(0));
    }

    [Test]
    public async Task ConflictKeepMine_ResolvesKeepLocalAndResyncs()
    {
        var conflict = new SyncConflict(SyncEntityKind.Item, Guid.NewGuid(), "Mine", "Theirs", 2, 2);
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, new[] { conflict })).Once()
            .Then.Returns(new SyncResult(1, 0, Array.Empty<SyncConflict>()));
        var vm = Make();
        await vm.SyncNowCommand.ExecuteAsync(null);

        await vm.Conflicts.Single().KeepMineCommand.ExecuteAsync(null);

        A.CallTo(() => _sync.ResolveAsync(conflict, true)).MustHaveHappenedOnceExactly();
        Assert.That(vm.HasConflicts, Is.False);
    }
}
