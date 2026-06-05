using Collectary.Infrastructure.Cloud;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// <see cref="ClientIdResolver"/> prefers an environment variable and falls back to the shipped
/// placeholder when it is missing or blank.
/// </summary>
[TestFixture]
public class ClientIdResolverTest
{
    private const string Var = "COLLECTARY_TEST_CLIENT_ID";
    private readonly ClientIdResolver _sut = new();

    [TearDown]
    public void TearDown() => Environment.SetEnvironmentVariable(Var, null);

    [Test]
    public void Resolve_EnvironmentVariableSet_ReturnsItsValue()
    {
        Environment.SetEnvironmentVariable(Var, "real-id");

        Assert.That(_sut.Resolve(Var, "fallback"), Is.EqualTo("real-id"));
    }

    [Test]
    public void Resolve_EnvironmentVariableUnset_ReturnsFallback()
    {
        Environment.SetEnvironmentVariable(Var, null);

        Assert.That(_sut.Resolve(Var, "fallback"), Is.EqualTo("fallback"));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Resolve_EnvironmentVariableBlank_ReturnsFallback(string blank)
    {
        Environment.SetEnvironmentVariable(Var, blank);

        Assert.That(_sut.Resolve(Var, "fallback"), Is.EqualTo("fallback"));
    }
}
