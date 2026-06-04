using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class AccountBootstrapperTest
{
    private IAuthService _auth = null!;
    private IUserRepository _users = null!;
    private IPresetRepository _presets = null!;
    private UserSession _session = null!;
    private AccountBootstrapper _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _auth = A.Fake<IAuthService>();
        _users = A.Fake<IUserRepository>();
        _presets = A.Fake<IPresetRepository>();
        _session = new UserSession();
        _sut = new AccountBootstrapper(_auth, _users, _presets, _session);
    }

    [Test]
    public async Task EnsureDefaultUserAsync_WhenExists_SetsSessionWithoutRegistering()
    {
        var user = new User { Username = AccountBootstrapper.DefaultUsername };
        A.CallTo(() => _users.GetByUsernameAsync(AccountBootstrapper.DefaultUsername)).Returns(user);

        var result = await _sut.EnsureDefaultUserAsync();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(user));
            Assert.That(_session.CurrentUser, Is.SameAs(user));
        });
        A.CallTo(() => _auth.RegisterAsync(A<string>._, A<string>._, A<string>._, A<string?>._)).MustNotHaveHappened();
    }

    [Test]
    public async Task EnsureDefaultUserAsync_WhenMissing_RegistersDefault()
    {
        A.CallTo(() => _users.GetByUsernameAsync(AccountBootstrapper.DefaultUsername)).Returns((User?)null);
        var registered = new User { Username = AccountBootstrapper.DefaultUsername };
        A.CallTo(() => _auth.RegisterAsync(AccountBootstrapper.DefaultUsername, "Default", A<string>._, A<string?>._))
            .Returns(registered);

        var result = await _sut.EnsureDefaultUserAsync();

        Assert.That(result, Is.SameAs(registered));
    }

    [Test]
    public async Task BackfillOwnerlessAsync_DelegatesToRepository()
    {
        var owner = Guid.NewGuid();

        await _sut.BackfillOwnerlessAsync(owner);

        A.CallTo(() => _presets.BackfillOwnerlessAsync(owner)).MustHaveHappenedOnceExactly();
    }
}
