namespace Collectary.Core.Ports;

public enum RuntimePermission
{
    Microphone,
    Camera
}

public interface IRuntimePermissions
{
    /// <summary>
    /// Ensures the given OS permission is granted, prompting the user the first time it is needed.
    /// Returns true once the permission is held, false if the user declined.
    /// </summary>
    Task<bool> RequestAsync(RuntimePermission permission);
}
