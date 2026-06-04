namespace Collectary.Core.Auth;

public class UsernameTakenException : Exception
{
    public UsernameTakenException(string username)
        : base($"The username '{username}' is already taken.")
    {
        Username = username;
    }

    public string Username { get; }
}
