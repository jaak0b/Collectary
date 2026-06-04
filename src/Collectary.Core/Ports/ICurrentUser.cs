namespace Collectary.Core.Ports;

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
}
