using Collectary.Core.Ports;
using Collectary.Presentation.Services;
using Collectary.Presentation.ViewModels;
using Collectary.UI.Tests.Infrastructure;
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
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0));
    }

    private SyncViewModel Make(IUiDispatcher? ui = null) => new(_sync, _status, ui ?? new InlineUiDispatcher(), new InlineBackgroundRunner());

    [Test]
    public async Task SyncNow_MarshalsUiStateThroughTheDispatcher()
    {
        var ui = new RecordingUiDispatcher();
        var vm = Make(ui);

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(ui.PostCount, Is.GreaterThan(0), "UI-state writes must go through the dispatcher");
            Assert.That(vm.LastSyncedAt, Is.Null, "state must not be applied until the dispatcher runs the posted action");
        });

        ui.Drain();

        Assert.That(vm.LastSyncedAt, Is.Not.Null, "draining the dispatcher applies the queued UI updates");
    }

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
    public void NeedsAttention_WhenClean_IsFalse()
    {
        Assert.That(Make().NeedsAttention, Is.False);
    }

    [Test]
    public async Task NeedsAttention_WhenSyncThrows_IsTrue()
    {
        A.CallTo(() => _sync.SyncAsync()).Throws(new InvalidOperationException("boom"));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.NeedsAttention, Is.True);
    }

    [Test]
    public async Task NeedsAttention_WhenErrorAppears_RaisesPropertyChanged()
    {
        A.CallTo(() => _sync.SyncAsync()).Throws(new InvalidOperationException("boom"));
        var vm = Make();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(changed, Does.Contain(nameof(vm.NeedsAttention)));
            Assert.That(changed, Does.Contain(nameof(vm.HasError)));
        });
    }
}
