namespace Collectary.Core.Auth;

/// <summary>
/// Thrown when a supplied password does not match the stored credential — e.g. when changing a
/// password without proving knowledge of the current one. Carries no detail that distinguishes
/// "no such credential" from "wrong password", to avoid leaking account state.
/// </summary>
public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The supplied credentials are invalid.")
    {
    }
}
