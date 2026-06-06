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
    public void Close_RaisesCloseRequested()
    {
        var vm = Make();
        var raised = false;
        vm.CloseRequested = () => raised = true;

        vm.CloseCommand.Execute(null);

        Assert.That(raised, Is.True);
    }

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
    public async Task SyncNow_WithUnresolvedConflicts_DoesNotStampLastSynced()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, new[]
        {
            new SyncConflict(SyncEntityKind.Preset, Guid.NewGuid(), "Mine", "Theirs", 2, 2),
        }));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.HasConflicts, Is.True);
            Assert.That(vm.LastSyncedAt, Is.Null, "must not claim a successful sync while conflicts remain");
        });
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
    public void LastSyncText_WhenLastSyncedSet_ReflectsTheTimestamp()
    {
        var vm = Make();
        var never = vm.LastSyncText;
        vm.LastSyncedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        Assert.That(vm.LastSyncText, Is.Not.EqualTo(never), "a synced timestamp must render differently from 'never'");
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
    public async Task ConflictResolution_WithMultipleConflicts_KeepsOthersAndDefersResync()
    {
        var c1 = new SyncConflict(SyncEntityKind.Item, Guid.NewGuid(), "M1", "T1", 2, 2);
        var c2 = new SyncConflict(SyncEntityKind.Item, Guid.NewGuid(), "M2", "T2", 2, 2);
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, new[] { c1, c2 }));
        var vm = Make();
        await vm.SyncNowCommand.ExecuteAsync(null);
        var secondVm = vm.Conflicts[1];

        await vm.Conflicts[0].KeepMineCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Conflicts, Has.Count.EqualTo(1), "resolving one conflict must not tear down the others");
            Assert.That(vm.Conflicts.Single(), Is.SameAs(secondVm), "the still-unresolved conflict instance must be preserved");
        });
        A.CallTo(() => _sync.SyncAsync()).MustHaveHappenedOnceExactly();
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
