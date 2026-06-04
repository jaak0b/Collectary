using Collectary.Core.Auth;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class LoginViewModelTest
{
    private IAuthService _auth = null!;
    private IAccountBootstrapper _bootstrapper = null!;
    private int _authenticatedCalls;

    [SetUp]
    public void SetUp()
    {
        _auth = A.Fake<IAuthService>();
        _bootstrapper = A.Fake<IAccountBootstrapper>();
        _authenticatedCalls = 0;
    }

    private LoginViewModel Make() => new(_auth, _bootstrapper, () => _authenticatedCalls++);

    [Test]
    public async Task Submit_Login_Success_InvokesCallbackAndBackfills()
    {
        var user = new User { Username = "alice" };
        A.CallTo(() => _auth.LoginAsync("alice", "pw")).Returns(user);
        var vm = Make();
        vm.Username = "alice";
        vm.Password = "pw";

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.That(_authenticatedCalls, Is.EqualTo(1));
        A.CallTo(() => _bootstrapper.BackfillOwnerlessAsync(user.Id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task Submit_Login_Failure_SetsErrorAndDoesNotAuthenticate()
    {
        A.CallTo(() => _auth.LoginAsync(A<string>._, A<string>._)).Returns((User?)null);
        var vm = Make();
        vm.Username = "x";
        vm.Password = "y";

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.HasError, Is.True);
            Assert.That(_authenticatedCalls, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task Submit_Register_Success_InvokesCallback()
    {
        var user = new User { Username = "alice" };
        A.CallTo(() => _auth.RegisterAsync("alice", "Alice", "pw", A<string?>._)).Returns(user);
        var vm = Make();
        vm.IsRegisterMode = true;
        vm.Username = "alice";
        vm.DisplayName = "Alice";
        vm.Password = "pw";

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.That(_authenticatedCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task Submit_Register_UsernameTaken_SetsError()
    {
        A.CallTo(() => _auth.RegisterAsync(A<string>._, A<string>._, A<string>._, A<string?>._))
            .Throws(new UsernameTakenException("alice"));
        var vm = Make();
        vm.IsRegisterMode = true;
        vm.Username = "alice";
        vm.Password = "pw";

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.That(vm.HasError, Is.True);
    }

    [Test]
    public async Task Submit_Register_ArgumentException_SetsError()
    {
        A.CallTo(() => _auth.RegisterAsync(A<string>._, A<string>._, A<string>._, A<string?>._))
            .Throws(new ArgumentException("bad"));
        var vm = Make();
        vm.IsRegisterMode = true;
        vm.Username = " ";
        vm.Password = "";

        await vm.SubmitCommand.ExecuteAsync(null);

        Assert.That(vm.HasError, Is.True);
    }

    [Test]
    public void ToggleMode_FlipsModeAndClearsError()
    {
        var vm = Make();
        vm.ErrorMessage = "boom";

        vm.ToggleModeCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsRegisterMode, Is.True);
            Assert.That(vm.HasError, Is.False);
        });
    }
}
