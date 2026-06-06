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
    private UserSession _session = null!;
    private ProfileService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _users = A.Fake<IUserRepository>();
        _session = new UserSession();
        _sut = new ProfileService(_users, _session);
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
