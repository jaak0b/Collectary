using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class ShareUseCaseTest
{
    private IShareRepository _shares = null!;
    private IUserRepository _users = null!;
    private IPresetRepository _presets = null!;
    private ICurrentUser _currentUser = null!;
    private ShareUseCase _sut = null!;
    private readonly Guid _me = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _shares = A.Fake<IShareRepository>();
        _users = A.Fake<IUserRepository>();
        _presets = A.Fake<IPresetRepository>();
        _currentUser = A.Fake<ICurrentUser>();
        A.CallTo(() => _currentUser.UserId).Returns(_me);
        _sut = new ShareUseCase(_shares, _users, _presets, _currentUser);
    }

    private Preset OwnedPreset(Guid? owner = null)
    {
        var preset = new Preset { OwnerId = owner ?? _me };
        A.CallTo(() => _presets.GetByIdAsync(preset.Id)).Returns(preset);
        return preset;
    }

    private User KnownUser(string username)
    {
        var user = new User { Username = username, DisplayName = username };
        A.CallTo(() => _users.GetByUsernameAsync(username)).Returns(user);
        A.CallTo(() => _users.GetByIdAsync(user.Id)).Returns(user);
        return user;
    }

    [Test]
    public async Task ShareAsync_WhenOwner_AddsShareWithPermission()
    {
        var preset = OwnedPreset();
        var bob = KnownUser("bob");

        await _sut.ShareAsync(preset.Id, "bob", SharePermission.Edit);

        A.CallTo(() => _shares.AddOrUpdateAsync(A<CollectionShare>.That.Matches(s =>
            s.PresetId == preset.Id &&
            s.SharedWithUserId == bob.Id &&
            s.GrantedByUserId == _me &&
            s.Permission == SharePermission.Edit))).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void ShareAsync_WhenNotOwner_Throws()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        KnownUser("bob");

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _sut.ShareAsync(preset.Id, "bob", SharePermission.Read));
    }

    [Test]
    public void ShareAsync_WhenPresetMissing_Throws()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _presets.GetByIdAsync(id)).Returns((Preset?)null);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ShareAsync(id, "bob", SharePermission.Read));
    }

    [Test]
    public void ShareAsync_WhenTargetUnknown_Throws()
    {
        var preset = OwnedPreset();
        A.CallTo(() => _users.GetByUsernameAsync("ghost")).Returns((User?)null);

        Assert.ThrowsAsync<UserNotFoundException>(
            () => _sut.ShareAsync(preset.Id, "ghost", SharePermission.Read));
    }

    [Test]
    public void ShareAsync_WithSelf_Throws()
    {
        var me = new User { Username = "me" };
        A.CallTo(() => _currentUser.UserId).Returns(me.Id);
        var preset = new Preset { OwnerId = me.Id };
        A.CallTo(() => _presets.GetByIdAsync(preset.Id)).Returns(preset);
        A.CallTo(() => _users.GetByUsernameAsync("me")).Returns(me);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.ShareAsync(preset.Id, "me", SharePermission.Read));
    }

    [Test]
    public async Task RevokeAsync_WhenOwner_RemovesShare()
    {
        var preset = OwnedPreset();
        var bob = KnownUser("bob");

        await _sut.RevokeAsync(preset.Id, "bob");

        A.CallTo(() => _shares.RemoveAsync(preset.Id, bob.Id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void RevokeAsync_WhenNotOwner_Throws()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        KnownUser("bob");

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.RevokeAsync(preset.Id, "bob"));
    }

    [Test]
    public async Task TransferAsync_WhenOwner_ChangesOwnerAndUpdates()
    {
        var preset = OwnedPreset();
        var bob = KnownUser("bob");

        await _sut.TransferAsync(preset.Id, "bob");

        Assert.That(preset.OwnerId, Is.EqualTo(bob.Id));
        A.CallTo(() => _presets.UpdateAsync(preset)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _shares.RemoveAsync(preset.Id, bob.Id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void TransferAsync_ToSelf_Throws()
    {
        var preset = OwnedPreset();
        var me = new User { Username = "me" };
        A.CallTo(() => _users.GetByUsernameAsync("me")).Returns(me);
        A.CallTo(() => _currentUser.UserId).Returns(me.Id);
        A.CallTo(() => _presets.GetByIdAsync(preset.Id)).Returns(new Preset { Id = preset.Id, OwnerId = me.Id });

        Assert.ThrowsAsync<InvalidOperationException>(() => _sut.TransferAsync(preset.Id, "me"));
    }

    [Test]
    public void TransferAsync_WhenNotOwner_Throws()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        KnownUser("bob");

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.TransferAsync(preset.Id, "bob"));
    }

    [Test]
    public async Task ListSharesAsync_ReturnsInfosForKnownUsers()
    {
        var preset = OwnedPreset();
        var bob = KnownUser("bob");
        var unknownUserId = Guid.NewGuid();
        A.CallTo(() => _users.GetByIdAsync(unknownUserId)).Returns((User?)null);
        A.CallTo(() => _shares.GetByPresetAsync(preset.Id)).Returns(new List<CollectionShare>
        {
            new() { PresetId = preset.Id, SharedWithUserId = bob.Id, Permission = SharePermission.Edit },
            new() { PresetId = preset.Id, SharedWithUserId = unknownUserId, Permission = SharePermission.Read },
        });

        var result = await _sut.ListSharesAsync(preset.Id);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Username, Is.EqualTo("bob"));
        Assert.That(result[0].Permission, Is.EqualTo(SharePermission.Edit));
    }

    [Test]
    public void ListSharesAsync_WhenNotOwner_Throws()
    {
        var preset = OwnedPreset(Guid.NewGuid());

        Assert.ThrowsAsync<UnauthorizedAccessException>(() => _sut.ListSharesAsync(preset.Id));
    }

    [Test]
    public async Task ListSharedWithMeAsync_ReturnsExistingPresets()
    {
        var preset = new Preset { OwnerId = Guid.NewGuid() };
        A.CallTo(() => _presets.GetByIdAsync(preset.Id)).Returns(preset);
        var missingPresetId = Guid.NewGuid();
        A.CallTo(() => _presets.GetByIdAsync(missingPresetId)).Returns((Preset?)null);
        A.CallTo(() => _shares.GetForUserAsync(_me)).Returns(new List<CollectionShare>
        {
            new() { PresetId = preset.Id, SharedWithUserId = _me },
            new() { PresetId = missingPresetId, SharedWithUserId = _me },
        });

        var result = await _sut.ListSharedWithMeAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.SameAs(preset));
    }
}
