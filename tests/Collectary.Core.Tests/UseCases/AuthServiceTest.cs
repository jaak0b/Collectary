using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class AuthServiceTest
{
    private IUserRepository _users = null!;
    private ICredentialStore _credentials = null!;
    private ICredentialHasher _hasher = null!;
    private UserSession _session = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _users = A.Fake<IUserRepository>();
        _credentials = A.Fake<ICredentialStore>();
        _hasher = A.Fake<ICredentialHasher>();
        _session = new UserSession();
        _sut = new AuthService(_users, _credentials, _hasher, _session);
    }

    private static PasswordHash AnyHash() => new(new byte[] { 1 }, new byte[] { 2 }, 1, "PBKDF2-HMAC-SHA512");

    [Test]
    public async Task ChangePasswordAsync_WithCorrectCurrentPassword_SavesNewHashForUser()
    {
        var userId = Guid.NewGuid();
        var current = AnyHash();
        var newHash = AnyHash();
        A.CallTo(() => _credentials.GetAsync(userId)).Returns(current);
        A.CallTo(() => _hasher.Verify("oldpw", current)).Returns(true);
        A.CallTo(() => _hasher.Hash("newpw")).Returns(newHash);

        await _sut.ChangePasswordAsync(userId, "oldpw", "newpw");

        A.CallTo(() => _credentials.SaveAsync(userId, newHash)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void ChangePasswordAsync_WithWrongCurrentPassword_ThrowsAndDoesNotSave()
    {
        var userId = Guid.NewGuid();
        var current = AnyHash();
        A.CallTo(() => _credentials.GetAsync(userId)).Returns(current);
        A.CallTo(() => _hasher.Verify("wrong", current)).Returns(false);

        Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.ChangePasswordAsync(userId, "wrong", "newpw"));
        A.CallTo(() => _credentials.SaveAsync(userId, A<PasswordHash>._)).MustNotHaveHappened();
    }

    [Test]
    public void ChangePasswordAsync_WhenNoCredentialStored_Throws()
    {
        var userId = Guid.NewGuid();
        A.CallTo(() => _credentials.GetAsync(userId)).Returns((PasswordHash?)null);

        Assert.ThrowsAsync<InvalidCredentialsException>(
            () => _sut.ChangePasswordAsync(userId, "oldpw", "newpw"));
    }

    [Test]
    public void ChangePasswordAsync_WhenNewPasswordEmpty_Throws() =>
        Assert.ThrowsAsync<ArgumentException>(() => _sut.ChangePasswordAsync(Guid.NewGuid(), "oldpw", ""));

    [Test]
    public async Task RegisterAsync_WhenUsernameFree_AddsUserAndSetsSession()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns((User?)null);
        A.CallTo(() => _hasher.Hash("pw")).Returns(AnyHash());

        var user = await _sut.RegisterAsync("alice", "Alice", "pw");

        Assert.Multiple(() =>
        {
            Assert.That(user.Username, Is.EqualTo("alice"));
            Assert.That(_session.CurrentUser, Is.SameAs(user));
        });
        A.CallTo(() => _users.AddAsync(user)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task RegisterAsync_SavesHashedCredentialForNewUser()
    {
        var hash = AnyHash();
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns((User?)null);
        A.CallTo(() => _hasher.Hash("pw")).Returns(hash);

        var user = await _sut.RegisterAsync("alice", "Alice", "pw");

        A.CallTo(() => _credentials.SaveAsync(user.Id, hash)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public void RegisterAsync_WhenUsernameTaken_Throws()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns(new User { Username = "alice" });

        Assert.ThrowsAsync<UsernameTakenException>(() => _sut.RegisterAsync("alice", "Alice", "pw"));
    }

    [Test]
    public void RegisterAsync_WithBlankUsername_Throws() =>
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync("  ", "X", "pw"));

    [Test]
    public void RegisterAsync_WithEmptyPassword_Throws() =>
        Assert.ThrowsAsync<ArgumentException>(() => _sut.RegisterAsync("alice", "X", ""));

    [Test]
    public async Task RegisterAsync_WithBlankDisplayName_DefaultsToUsername()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns((User?)null);
        A.CallTo(() => _hasher.Hash(A<string>._)).Returns(AnyHash());

        var user = await _sut.RegisterAsync("alice", "   ", "pw");

        Assert.That(user.DisplayName, Is.EqualTo("alice"));
    }

    [Test]
    public async Task RegisterAsync_WithBlankEmail_StoresNull()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns((User?)null);
        A.CallTo(() => _hasher.Hash(A<string>._)).Returns(AnyHash());

        var user = await _sut.RegisterAsync("alice", "Alice", "pw", "  ");

        Assert.That(user.Email, Is.Null);
    }

    [Test]
    public async Task RegisterAsync_WithValidEmail_StoresIt()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns((User?)null);
        A.CallTo(() => _hasher.Hash(A<string>._)).Returns(AnyHash());

        var user = await _sut.RegisterAsync("alice", "Alice", "pw", "alice@example.com");

        Assert.That(user.Email, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public void RegisterAsync_WithInvalidEmail_Throws()
    {
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns((User?)null);

        Assert.ThrowsAsync<ArgumentException>(
            () => _sut.RegisterAsync("alice", "Alice", "pw", "not-an-email"));
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ReturnsUserAndSetsSession()
    {
        var user = new User { Username = "alice" };
        var hash = AnyHash();
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns(user);
        A.CallTo(() => _credentials.GetAsync(user.Id)).Returns(hash);
        A.CallTo(() => _hasher.Verify("pw", hash)).Returns(true);

        var result = await _sut.LoginAsync("alice", "pw");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(user));
            Assert.That(_session.IsAuthenticated, Is.True);
        });
    }

    [Test]
    public async Task LoginAsync_WithUnknownUser_ReturnsNull()
    {
        A.CallTo(() => _users.GetByUsernameAsync("ghost")).Returns((User?)null);

        var result = await _sut.LoginAsync("ghost", "pw");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(_session.IsAuthenticated, Is.False);
        });
    }

    [Test]
    public async Task LoginAsync_WithUnknownUser_StillRunsAHashVerify()
    {
        // Equalises timing between existing and non-existing usernames so the response
        // time cannot be used to enumerate accounts.
        A.CallTo(() => _users.GetByUsernameAsync("ghost")).Returns((User?)null);

        await _sut.LoginAsync("ghost", "pw");

        A.CallTo(() => _hasher.Verify("pw", A<PasswordHash>._)).MustHaveHappened();
    }

    [Test]
    public async Task LoginAsync_WithMissingCredential_ReturnsNull()
    {
        var user = new User { Username = "alice" };
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns(user);
        A.CallTo(() => _credentials.GetAsync(user.Id)).Returns((PasswordHash?)null);

        var result = await _sut.LoginAsync("alice", "pw");

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        var user = new User { Username = "alice" };
        var hash = AnyHash();
        A.CallTo(() => _users.GetByUsernameAsync("alice")).Returns(user);
        A.CallTo(() => _credentials.GetAsync(user.Id)).Returns(hash);
        A.CallTo(() => _hasher.Verify("pw", hash)).Returns(false);

        var result = await _sut.LoginAsync("alice", "pw");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Null);
            Assert.That(_session.IsAuthenticated, Is.False);
        });
    }

    [Test]
    public async Task Logout_ClearsSession()
    {
        _session.SetCurrentUser(new User { Username = "alice" });

        _sut.Logout();

        Assert.That(_session.IsAuthenticated, Is.False);
    }

    [Test]
    public void CurrentUser_ReflectsSession()
    {
        var user = new User { Username = "alice" };
        _session.SetCurrentUser(user);

        Assert.That(_sut.CurrentUser, Is.SameAs(user));
    }
}
