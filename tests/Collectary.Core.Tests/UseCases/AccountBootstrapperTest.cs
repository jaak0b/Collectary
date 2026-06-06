using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class AccountBootstrapperTest
{
    private IPresetRepository _presets = null!;
    private AccountBootstrapper _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _presets = A.Fake<IPresetRepository>();
        _sut = new AccountBootstrapper(_presets);
    }

    [Test]
    public async Task BackfillOwnerlessAsync_DelegatesToRepository()
    {
        var owner = Guid.NewGuid();

        await _sut.BackfillOwnerlessAsync(owner);

        A.CallTo(() => _presets.BackfillOwnerlessAsync(owner)).MustHaveHappenedOnceExactly();
    }
}
