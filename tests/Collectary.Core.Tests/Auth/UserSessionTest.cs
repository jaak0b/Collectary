using Collectary.Core.Auth;
using Collectary.Core.Domain;

namespace Collectary.Core.Tests.Auth;

[TestFixture]
public class UserSessionTest
{
    [Test]
    public void New_IsNotAuthenticated()
    {
        var session = new UserSession();

        Assert.Multiple(() =>
        {
            Assert.That(session.IsAuthenticated, Is.False);
            Assert.That(session.UserId, Is.EqualTo(Guid.Empty));
            Assert.That(session.CurrentUser, Is.Null);
        });
    }

    [Test]
    public void SetCurrentUser_MakesAuthenticatedWithUserId()
    {
        var session = new UserSession();
        var user = new User { Username = "alice" };

        session.SetCurrentUser(user);

        Assert.Multiple(() =>
        {
            Assert.That(session.IsAuthenticated, Is.True);
            Assert.That(session.UserId, Is.EqualTo(user.Id));
            Assert.That(session.CurrentUser, Is.SameAs(user));
        });
    }

    [Test]
    public void Clear_ResetsToUnauthenticated()
    {
        var session = new UserSession();
        session.SetCurrentUser(new User { Username = "bob" });

        session.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(session.IsAuthenticated, Is.False);
            Assert.That(session.UserId, Is.EqualTo(Guid.Empty));
        });
    }
}
