namespace Collectary.Core.Auth;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string username)
        : base($"No user named '{username}' was found.")
    {
        Username = username;
    }

    public string Username { get; }
}
