using Collectary.Core.Domain;
using Collectary.Core.Ports;

namespace Collectary.Core.Auth;

public class UserSession : ICurrentUser
{
    public User? CurrentUser { get; private set; }

    public Guid UserId => CurrentUser?.Id ?? Guid.Empty;

    public bool IsAuthenticated => CurrentUser is not null;

    public void SetCurrentUser(User user) => CurrentUser = user;

    public void Clear() => CurrentUser = null;
}
