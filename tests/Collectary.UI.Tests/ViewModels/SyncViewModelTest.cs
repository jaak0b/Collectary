using Collectary.Core.Ports;
using Collectary.Presentation.Localization;
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
        A.CallTo(() => _status.LocationLabel).Returns("Sync folder");
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0));
    }

    private SyncViewModel Make(IUiDispatcher? ui = null) => new(_sync, _status, ui ?? new InlineUiDispatcher(), new InlineBackgroundRunner());

    [Test]
    public void SyncNow_MarshalsUiStateThroughTheDispatcher()
    {
        var ui = new RecordingUiDispatcher();
        var vm = Make(ui);

        var task = vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(ui.PostCount, Is.GreaterThan(0), "UI-state writes must go through the dispatcher");
            Assert.That(vm.LastSyncedAt, Is.Null, "state must not be applied until the dispatcher runs the posted action");
            Assert.That(task.IsCompleted, Is.False, "the command must not finish until the dispatcher runs its final update");
        });

        ui.Drain();

        Assert.Multiple(() =>
        {
            Assert.That(vm.LastSyncedAt, Is.Not.Null, "draining the dispatcher applies the queued UI updates");
            Assert.That(task.IsCompleted, Is.True, "the command completes once its final UI update has run");
        });
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
    public void RequestSync_InvokesTheCommandThroughTheUiDispatcher_NotOnTheCallingThread()
    {
        var ui = new RecordingUiDispatcher();
        var vm = Make(ui);

        _ = vm.RequestSyncAsync();

        A.CallTo(() => _sync.SyncAsync()).MustNotHaveHappened();

        ui.Drain();

        A.CallTo(() => _sync.SyncAsync()).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void SyncNow_CompletesThroughTheUiDispatcher_SoCanExecuteChangedFiresOnTheUiThread()
    {
        var ui = new RecordingUiDispatcher();
        var vm = Make(ui);

        var task = vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(task.IsCompleted, Is.False,
            "the command must not finish until the UI dispatcher has run its final update; otherwise the AsyncRelayCommand raises CanExecuteChanged off the UI thread and a bound Button crashes Avalonia");

        ui.Drain();

        Assert.That(task.IsCompleted, Is.True, "draining the dispatcher lets the command complete on the UI thread");
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

        Assert.That(vm.NeedsAttention, Is.True);
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
    public void Severity_WhenClean_IsNone()
    {
        var vm = Make();
        Assert.Multiple(() =>
        {
            Assert.That(vm.Severity, Is.EqualTo(SyncNoticeSeverity.None));
            Assert.That(vm.IsError, Is.False);
        });
    }

    [Test]
    public void SettingSeverity_RaisesPropertyChangedForDerivedFlags()
    {
        var vm = Make();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.Severity = SyncNoticeSeverity.Advisory;

        Assert.Multiple(() =>
        {
            Assert.That(changed, Does.Contain(nameof(vm.NeedsAttention)));
            Assert.That(changed, Does.Contain(nameof(vm.IsError)));
        });
    }

    [Test]
    public void IsError_TracksSeverity()
    {
        var vm = Make();

        vm.Severity = SyncNoticeSeverity.Advisory;
        Assert.That(vm.IsError, Is.False);

        vm.Severity = SyncNoticeSeverity.Error;
        Assert.That(vm.IsError, Is.True);
    }

    [Test]
    public async Task SyncNow_AfterACleanSync_ShowsThePushedAndPulledCounts()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(2, 3));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.LastResultText, Does.Contain("2").And.Contain("3"),
            "the user must see how many records were pushed and pulled");
    }

    [Test]
    public async Task SyncNow_WhenTheSyncFails_LeavesNoStaleResultText()
    {
        var vm = Make();
        await vm.SyncNowCommand.ExecuteAsync(null);
        A.CallTo(() => _sync.SyncAsync()).Throws(new InvalidOperationException("boom"));

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.LastResultText, Is.Null, "a failed sync must not display the previous run's counts");
    }

    [Test]
    public async Task SyncNow_WhenBackendUnavailable_ShowsNoResultText()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, BackendUnavailable: true));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.LastResultText, Is.Null, "an unavailable location produced no transfer to report");
    }

    [Test]
    public async Task Severity_WhenBackendUnavailable_IsAdvisoryNotError()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, BackendUnavailable: true));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.Severity, Is.EqualTo(SyncNoticeSeverity.Advisory),
            "an unreachable location is a retry-able advisory, not a hard failure");
    }

    [Test]
    public async Task Severity_WhenPartialSync_IsAdvisory()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, Skipped: 1));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.Severity, Is.EqualTo(SyncNoticeSeverity.Advisory));
    }

    [Test]
    public async Task Severity_WhenSyncThrows_IsError()
    {
        A.CallTo(() => _sync.SyncAsync()).Throws(new InvalidOperationException("boom"));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.Severity, Is.EqualTo(SyncNoticeSeverity.Error));
    }

    [Test]
    public async Task Severity_AfterPartialThenCleanSync_ResetsToNone()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, Skipped: 1));
        var vm = Make();
        await vm.SyncNowCommand.ExecuteAsync(null);
        Assume.That(vm.Severity, Is.EqualTo(SyncNoticeSeverity.Advisory));

        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0));
        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Severity, Is.EqualTo(SyncNoticeSeverity.None));
            Assert.That(vm.NeedsAttention, Is.False);
        });
    }

    [Test]
    public void ReportError_SurfacesTheGenericSyncErrorNotice()
    {
        var vm = Make();

        vm.ReportError();

        Assert.That(vm.NeedsAttention, Is.True, "a surfaced scheduler failure must flag attention");
    }

    [Test]
    public async Task SyncNow_WhenNothingSkipped_LeavesNoPartialNotice()
    {
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.NeedsAttention, Is.False, "a clean sync (nothing skipped) must not raise a partial-sync notice");
    }

    [Test]
    public async Task SyncNow_WhenSomeEntitiesSkipped_FlagsAttentionButStillRecordsTheSync()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 3, 2));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.NeedsAttention, Is.True, "a partial sync must visibly flag that some items could not be applied");
            Assert.That(vm.ErrorMessage, Does.Contain("2"), "the notice reports how many items could not be applied");
            Assert.That(vm.LastSyncedAt, Is.Not.Null, "the sync still completed, so the timestamp updates");
        });
    }

    [Test]
    public async Task SyncNow_WhenMultipleIssuesInGerman_JoinsClausesWithTheLocalizedSeparator()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, Skipped: 2, ImagesFailed: 3));
        var vm = Make();
        try
        {
            LocalizationService.Instance.Apply("de");

            await vm.SyncNowCommand.ExecuteAsync(null);

            Assert.That(vm.ErrorMessage, Does.Contain(" und "),
                "German joins the issue clauses with 'und', not a comma splice");
        }
        finally
        {
            LocalizationService.Instance.Apply("en");
        }
    }

    [Test]
    public async Task SyncNow_WhenBackendUnavailable_ShowsNoticeAndDoesNotRecordASync()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, BackendUnavailable: true));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.LastSyncedAt, Is.Null, "an unavailable backend did not sync, so it must not look successful");
            Assert.That(vm.NeedsAttention, Is.True, "the user is told the sync location was unreachable");
        });
    }

    [Test]
    public async Task SyncNow_WhenBackendUnavailable_NamesTheConfiguredLocation()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, BackendUnavailable: true));
        A.CallTo(() => _status.LocationLabel).Returns("OneDrive (Collectary)");
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.That(vm.ErrorMessage, Does.Contain("OneDrive (Collectary)"),
            "the unreachable notice tells the user which location failed");
    }

    [Test]
    public async Task SyncNow_WhenADeviceWasUnreadable_FlagsAttentionButStillRecordsTheSync()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 1, 0, UnreadableDevices: 1));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.NeedsAttention, Is.True, "an excluded peer device must be surfaced");
            Assert.That(vm.ErrorMessage, Does.Contain("1"), "the notice reports how many device files were unreadable");
            Assert.That(vm.LastSyncedAt, Is.Not.Null, "the sync still ran, so the timestamp updates");
        });
    }

    [Test]
    public async Task SyncNow_WhenAnImageFailed_FlagsAttentionButStillRecordsTheSync()
    {
        A.CallTo(() => _sync.SyncAsync()).Returns(new SyncResult(0, 0, 0, ImagesFailed: 2));
        var vm = Make();

        await vm.SyncNowCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.NeedsAttention, Is.True, "an image that could not transfer must be surfaced");
            Assert.That(vm.ErrorMessage, Does.Contain("2"), "the notice reports how many images failed to transfer");
            Assert.That(vm.LastSyncedAt, Is.Not.Null);
        });
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
            Assert.That(changed, Does.Contain(nameof(vm.ErrorMessage)));
        });
    }
}
