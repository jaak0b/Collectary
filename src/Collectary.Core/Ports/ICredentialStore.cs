namespace Collectary.Core.Ports;

public interface ICredentialStore
{
    Task SaveAsync(Guid userId, PasswordHash credential);
    Task<PasswordHash?> GetAsync(Guid userId);
}
