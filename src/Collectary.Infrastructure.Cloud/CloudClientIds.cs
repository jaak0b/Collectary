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
}
