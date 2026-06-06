using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Presentation.ViewModels;
using FakeItEasy;

namespace Collectary.UI.Tests.ViewModels;

[TestFixture]
public class ProfilePickerViewModelTest
{
    private IProfileService _profiles = null!;
    private User? _selected;

    [SetUp]
    public void SetUp()
    {
        _profiles = A.Fake<IProfileService>();
        _selected = null;
    }

    private ProfilePickerViewModel Make() => new(_profiles, user =>
    {
        _selected = user;
        return Task.CompletedTask;
    });

    [Test]
    public async Task LoadAsync_PopulatesTiles()
    {
        A.CallTo(() => _profiles.GetProfilesAsync()).Returns(new List<User>
        {
            new() { DisplayName = "Alice" },
            new() { DisplayName = "Bob" },
        });
        var vm = Make();

        await vm.LoadAsync();

        Assert.That(vm.Profiles.Select(t => t.Name), Is.EqualTo(new[] { "Alice", "Bob" }));
    }

    [Test]
    public async Task SelectProfileCommand_InvokesCallbackWithUser()
    {
        var user = new User { DisplayName = "Alice" };
        var vm = Make();

        await vm.SelectProfileCommand.ExecuteAsync(new ProfileTileViewModel(user));

        Assert.That(_selected, Is.SameAs(user));
    }

    [Test]
    public void BeginAddCommand_EntersAddingState()
    {
        var vm = Make();

        vm.BeginAddCommand.Execute(null);

        Assert.That(vm.IsAdding, Is.True);
    }

    [Test]
    public void CancelAddCommand_LeavesAddingState()
    {
        var vm = Make();
        vm.BeginAddCommand.Execute(null);
        vm.NewProfileName = "Carol";

        vm.CancelAddCommand.Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(vm.IsAdding, Is.False);
            Assert.That(vm.NewProfileName, Is.Empty);
        });
    }

    [Test]
    public async Task CreateProfileCommand_CreatesAndSelects()
    {
        var created = new User { DisplayName = "Carol" };
        A.CallTo(() => _profiles.CreateProfileAsync("Carol")).Returns(created);
        var vm = Make();
        vm.NewProfileName = "Carol";

        await vm.CreateProfileCommand.ExecuteAsync(null);

        A.CallTo(() => _profiles.CreateProfileAsync("Carol")).MustHaveHappenedOnceExactly();
        Assert.That(_selected, Is.SameAs(created));
    }

    [Test]
    public async Task CreateProfileCommand_WithBlankName_SetsErrorAndDoesNotCreate()
    {
        var vm = Make();
        vm.BeginAddCommand.Execute(null);
        vm.NewProfileName = "   ";

        await vm.CreateProfileCommand.ExecuteAsync(null);

        A.CallTo(() => _profiles.CreateProfileAsync(A<string>._)).MustNotHaveHappened();
        Assert.Multiple(() =>
        {
            Assert.That(vm.HasError, Is.True);
            Assert.That(_selected, Is.Null);
        });
    }
}
