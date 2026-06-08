using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class ProfileServiceTest
{
    private IUserRepository _users = null!;
    private IPresetUseCase _presets = null!;
    private UserSession _session = null!;
    private ProfileService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _users = A.Fake<IUserRepository>();
        _presets = A.Fake<IPresetUseCase>();
        _session = new UserSession();
        _sut = new ProfileService(_users, _session, _presets);
    }

    private User SignIn(string username = "me")
    {
        var user = new User { Username = username, DisplayName = username };
        _session.SetCurrentUser(user);
        return user;
    }

    [Test]
    public async Task GetProfilesAsync_ReturnsAllUsers()
    {
        var people = new List<User> { new() { Username = "alice" }, new() { Username = "bob" } };
        A.CallTo(() => _users.GetAllAsync()).Returns(people);

        var result = await _sut.GetProfilesAsync();

        Assert.That(result, Is.EquivalentTo(people));
    }

    [Test]
    public async Task CreateProfileAsync_SetsUsernameAndDisplayNameFromName()
    {
        A.CallTo(() => _users.GetByUsernameAsync(A<string>._)).Returns((User?)null);

        var user = await _sut.CreateProfileAsync("  Alice  ");

        Assert.Multiple(() =>
        {
            Assert.That(user.DisplayName, Is.EqualTo("Alice"));
            Assert.That(user.Username, Is.EqualTo("alice"));
        });
        A.CallTo(() => _users.AddAsync(user)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task CreateProfileAsync_OnDuplicateName_SuffixesUsername()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns(new User { Username = "alice" });
        A.CallTo(() => _users.GetByUsernameAsync("alice-2")).Returns((User?)null);

        var user = await _sut.CreateProfileAsync("Alice");

        Assert.Multiple(() =>
        {
            Assert.That(user.Username, Is.EqualTo("alice-2"));
            Assert.That(user.DisplayName, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void CreateProfileAsync_WithBlankName_Throws() =>
        Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateProfileAsync("   "));

    [Test]
    public async Task CreateProfileAsync_NormalizesWhitespaceAndConnectorsToDashes()
    {
        A.CallTo(() => _users.GetByUsernameAsync(A<string>._)).Returns((User?)null);

        var spaced = (await _sut.CreateProfileAsync("Alice Smith")).Username;
        var connectored = (await _sut.CreateProfileAsync("Bob_Jones")).Username;

        Assert.Multiple(() =>
        {
            Assert.That(spaced, Is.EqualTo("alice-smith"), "whitespace becomes a dash in the username slug");
            Assert.That(connectored, Is.EqualTo("bob-jones"), "underscores become dashes in the username slug");
        });
    }

    [Test]
    public async Task CreateProfileAsync_WhenNameHasNoAlphanumerics_FallsBackToProfile()
    {
        A.CallTo(() => _users.GetByUsernameAsync(A<string>._)).Returns((User?)null);

        var user = await _sut.CreateProfileAsync("!!!");

        Assert.That(user.Username, Is.EqualTo("profile"), "a name that slugs to empty falls back to 'profile'");
    }

    [Test]
    public async Task CreateProfileAsync_DoesNotChangeSession()
    {
        A.CallTo(() => _users.GetByUsernameAsync(A<string>._)).Returns((User?)null);

        await _sut.CreateProfileAsync("Alice");

        Assert.That(_session.IsAuthenticated, Is.False);
    }

    [Test]
    public void SelectProfile_SetsSession()
    {
        var user = new User { Username = "alice" };

        _sut.SelectProfile(user);

        Assert.Multiple(() =>
        {
            Assert.That(_session.CurrentUser, Is.SameAs(user));
            Assert.That(_sut.CurrentProfile, Is.SameAs(user));
        });
    }

    [Test]
    public async Task CountOwnedCollectionsAsync_CountsOnlyCurrentProfilesPresets()
    {
        var me = SignIn();
        A.CallTo(() => _presets.GetAllPresetsAsync()).Returns(new List<Preset>
        {
            new() { OwnerId = me.Id },
            new() { OwnerId = me.Id },
            new() { OwnerId = Guid.NewGuid() },
        });

        Assert.That(await _sut.CountOwnedCollectionsAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task DeleteCurrentProfileAsync_DeletesEveryOwnedCollectionThenTheProfile()
    {
        var me = SignIn();
        var mine1 = new Preset { Id = Guid.NewGuid(), OwnerId = me.Id };
        var mine2 = new Preset { Id = Guid.NewGuid(), OwnerId = me.Id };
        var theirs = new Preset { Id = Guid.NewGuid(), OwnerId = Guid.NewGuid() };
        A.CallTo(() => _presets.GetAllPresetsAsync()).Returns(new List<Preset> { mine1, mine2, theirs });

        await _sut.DeleteCurrentProfileAsync();

        A.CallTo(() => _presets.DeletePresetAsync(mine1.Id)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _presets.DeletePresetAsync(mine2.Id)).MustHaveHappenedOnceExactly();
        A.CallTo(() => _presets.DeletePresetAsync(theirs.Id)).MustNotHaveHappened();
        A.CallTo(() => _users.DeleteAsync(me.Id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task DeleteCurrentProfileAsync_DeletesChildCollectionsBeforeTheirParents()
    {
        var me = SignIn();
        var parent = new Preset { Id = Guid.NewGuid(), OwnerId = me.Id };
        var child = new Preset { Id = Guid.NewGuid(), OwnerId = me.Id, ParentPresetId = parent.Id };
        A.CallTo(() => _presets.GetAllPresetsAsync()).Returns(new List<Preset> { parent, child });

        await _sut.DeleteCurrentProfileAsync();

        A.CallTo(() => _presets.DeletePresetAsync(child.Id)).MustHaveHappened()
            .Then(A.CallTo(() => _presets.DeletePresetAsync(parent.Id)).MustHaveHappened());
    }

    [Test]
    public async Task DeleteCurrentProfileAsync_WhenNoCurrentProfile_DoesNothing()
    {
        await _sut.DeleteCurrentProfileAsync();

        A.CallTo(() => _users.DeleteAsync(A<Guid>._)).MustNotHaveHappened();
        A.CallTo(() => _presets.DeletePresetAsync(A<Guid>._)).MustNotHaveHappened();
    }

    [Test]
    public void SignOut_ClearsSession()
    {
        _session.SetCurrentUser(new User { Username = "alice" });

        _sut.SignOut();

        Assert.Multiple(() =>
        {
            Assert.That(_session.IsAuthenticated, Is.False);
            Assert.That(_sut.CurrentProfile, Is.Null);
        });
    }
}
