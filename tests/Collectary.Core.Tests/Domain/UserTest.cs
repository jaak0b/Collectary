using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Domain;

[TestFixture]
public class UserTest
{
    [Test]
    public void Username_DefaultsToEmptyString() =>
        Assert.That(new User().Username, Is.EqualTo(string.Empty));

    [Test]
    public void DisplayName_DefaultsToEmptyString() =>
        Assert.That(new User().DisplayName, Is.EqualTo(string.Empty));

    [Test]
    public void Id_DefaultsToNonEmptyGuid() =>
        Assert.That(new User().Id, Is.Not.EqualTo(Guid.Empty));
}
