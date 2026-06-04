using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using FakeItEasy;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class CollectionAuthorizationServiceTest
{
    private IPresetRepository _presets = null!;
    private IShareRepository _shares = null!;
    private ICurrentUser _currentUser = null!;
    private CollectionAuthorizationService _sut = null!;
    private readonly Guid _me = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _presets = A.Fake<IPresetRepository>();
        _shares = A.Fake<IShareRepository>();
        _currentUser = A.Fake<ICurrentUser>();
        A.CallTo(() => _currentUser.UserId).Returns(_me);
        _sut = new CollectionAuthorizationService(_presets, _shares, _currentUser);
    }

    private Preset OwnedPreset(Guid? owner = null)
    {
        var preset = new Preset { OwnerId = owner ?? _me };
        A.CallTo(() => _presets.GetByIdAsync(preset.Id)).Returns(preset);
        return preset;
    }

    [Test]
    public async Task IsOwnerAsync_WhenOwner_True() =>
        Assert.That(await _sut.IsOwnerAsync(OwnedPreset().Id), Is.True);

    [Test]
    public async Task IsOwnerAsync_WhenNotOwner_False() =>
        Assert.That(await _sut.IsOwnerAsync(OwnedPreset(Guid.NewGuid()).Id), Is.False);

    [Test]
    public async Task IsOwnerAsync_WhenPresetMissing_False()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _presets.GetByIdAsync(id)).Returns((Preset?)null);

        Assert.That(await _sut.IsOwnerAsync(id), Is.False);
    }

    [Test]
    public async Task CanReadAsync_WhenOwner_True() =>
        Assert.That(await _sut.CanReadAsync(OwnedPreset().Id), Is.True);

    [Test]
    public async Task CanReadAsync_WhenReadShareExists_True()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        A.CallTo(() => _shares.GetAsync(preset.Id, _me))
            .Returns(new CollectionShare { Permission = SharePermission.Read });

        Assert.That(await _sut.CanReadAsync(preset.Id), Is.True);
    }

    [Test]
    public async Task CanReadAsync_WhenNoAccess_False()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        A.CallTo(() => _shares.GetAsync(preset.Id, _me)).Returns((CollectionShare?)null);

        Assert.That(await _sut.CanReadAsync(preset.Id), Is.False);
    }

    [Test]
    public async Task CanReadAsync_WhenPresetMissing_False()
    {
        var id = Guid.NewGuid();
        A.CallTo(() => _presets.GetByIdAsync(id)).Returns((Preset?)null);

        Assert.That(await _sut.CanReadAsync(id), Is.False);
    }

    [Test]
    public async Task CanWriteAsync_WhenOwner_True() =>
        Assert.That(await _sut.CanWriteAsync(OwnedPreset().Id), Is.True);

    [Test]
    public async Task CanWriteAsync_WhenEditShare_True()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        A.CallTo(() => _shares.GetAsync(preset.Id, _me))
            .Returns(new CollectionShare { Permission = SharePermission.Edit });

        Assert.That(await _sut.CanWriteAsync(preset.Id), Is.True);
    }

    [Test]
    public async Task CanWriteAsync_WhenReadShare_False()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        A.CallTo(() => _shares.GetAsync(preset.Id, _me))
            .Returns(new CollectionShare { Permission = SharePermission.Read });

        Assert.That(await _sut.CanWriteAsync(preset.Id), Is.False);
    }

    [Test]
    public async Task CanWriteAsync_WhenNoShare_False()
    {
        var preset = OwnedPreset(Guid.NewGuid());
        A.CallTo(() => _shares.GetAsync(preset.Id, _me)).Returns((CollectionShare?)null);

        Assert.That(await _sut.CanWriteAsync(preset.Id), Is.False);
    }
}
