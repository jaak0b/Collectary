using FakeItEasy;
using Collectary.Core.Domain;
using Collectary.Core.Domain.Fields;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class SystemFieldUseCaseTest
{
    private ISystemFieldRepository _repo = null!;
    private SystemFieldUseCase _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = A.Fake<ISystemFieldRepository>();
        _sut = new SystemFieldUseCase(_repo);
    }

    private static SystemField MakeField() =>
        new() { Definition = new TextFieldDefinition { Label = "Test" } };

    [Test]
    public async Task GetAllAsync_ReturnsRepositoryResult()
    {
        var fields = new List<SystemField> { MakeField(), MakeField() };
        A.CallTo(() => _repo.GetAllAsync()).Returns(fields);

        var result = await _sut.GetAllAsync();

        Assert.That(result, Is.EqualTo(fields));
    }

    [Test]
    public async Task GetByIdAsync_ReturnsRepositoryResult()
    {
        var id = Guid.NewGuid();
        var field = MakeField();
        A.CallTo(() => _repo.GetByIdAsync(id)).Returns(field);

        var result = await _sut.GetByIdAsync(id);

        Assert.That(result, Is.EqualTo(field));
    }

    [Test]
    public async Task CreateAsync_DelegatesToRepository()
    {
        var field = MakeField();

        await _sut.CreateAsync(field);

        A.CallTo(() => _repo.AddAsync(field)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task UpdateAsync_DelegatesToRepository()
    {
        var field = MakeField();

        await _sut.UpdateAsync(field);

        A.CallTo(() => _repo.UpdateAsync(field)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task DeleteAsync_DelegatesToRepository()
    {
        var id = Guid.NewGuid();

        await _sut.DeleteAsync(id);

        A.CallTo(() => _repo.DeleteAsync(id)).MustHaveHappenedOnceExactly();
    }

    [Test]
    public async Task ReorderAsync_PassesOrderedIdsToRepository()
    {
        var orderedIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        await _sut.ReorderAsync(orderedIds);

        A.CallTo(() => _repo.ReorderAsync(orderedIds)).MustHaveHappenedOnceExactly();
    }
}
