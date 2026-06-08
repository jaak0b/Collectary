using Collectary.Core.UseCases;

namespace Collectary.Core.Tests.UseCases;

[TestFixture]
public class UsernameUniquifierTest
{
    private readonly UsernameUniquifier _sut = new();

    private Func<string, Task<bool>> Taken(params string[] taken)
    {
        var set = taken.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return candidate => Task.FromResult(set.Contains(candidate));
    }

    [Test]
    public async Task MakeUniqueAsync_WhenBaseIsFree_ReturnsBaseUnchanged()
    {
        var result = await _sut.MakeUniqueAsync("alice", Taken());

        Assert.That(result, Is.EqualTo("alice"));
    }

    [Test]
    public async Task MakeUniqueAsync_WhenBaseIsTaken_AppendsCounterStartingAtTwo()
    {
        var result = await _sut.MakeUniqueAsync("alice", Taken("alice"));

        Assert.That(result, Is.EqualTo("alice-2"));
    }

    [Test]
    public async Task MakeUniqueAsync_SkipsEveryTakenCandidate()
    {
        var result = await _sut.MakeUniqueAsync("alice", Taken("alice", "alice-2", "alice-3"));

        Assert.That(result, Is.EqualTo("alice-4"));
    }

    [Test]
    public async Task MakeUniqueAsync_IsCaseInsensitiveViaThePredicate()
    {
        var result = await _sut.MakeUniqueAsync("Alice", Taken("alice"));

        Assert.That(result, Is.EqualTo("Alice-2"), "a case-insensitive collision still uniquifies, preserving the caller's casing");
    }
}
