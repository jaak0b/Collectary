namespace Collectary.Infrastructure.Cloud;

/// <summary>
/// Public OAuth client identifiers shipped with the app. These are <em>native/public</em> PKCE
/// clients — there is deliberately no client secret. Replace the placeholders with the real ids
/// from the one-time developer app registrations (Azure Entra / Google Cloud).
/// </summary>
public class CloudClientIds
{
    public const string OneDrive = "<ONEDRIVE_CLIENT_ID>";
    public const string GoogleDrive = "<GOOGLE_CLIENT_ID>";

    // Google "Desktop app" OAuth clients are issued a client secret. For installed apps it is not
    // treated as confidential (it ships with the app), so this is not a real secret.
    public const string GoogleDriveSecret = "<GOOGLE_CLIENT_SECRET>";
}
