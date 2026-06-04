namespace Collectary.Core.Ports;

public interface ICredentialHasher
{
    PasswordHash Hash(string password);
    bool Verify(string password, PasswordHash stored);
}
