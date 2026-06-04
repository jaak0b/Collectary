using Autofac;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Cloud.GoogleDrive;
using Collectary.Infrastructure.Cloud.OneDrive;
using Collectary.Infrastructure.Sync;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Collectary.Infrastructure.Cloud;

/// <summary>
/// Desktop-only DI for cloud sync providers. Registered by <c>Collectary.UI.Desktop</c> (never by the
/// Browser build), so the MSAL/Graph/Google SDKs stay out of the WebAssembly graph. Each provider
/// exposes a keyed <see cref="ICloudAuthClient"/>, <see cref="ICloudFileStore"/>/
/// <see cref="ICloudRootProvider"/>, and an <see cref="ISyncBackend"/> the routing backend selects.
/// </summary>
public class CloudModule : Module
{
    private readonly string _tokenCacheDirectory;
    private readonly Func<string?> _oneDriveRootFolderId;
    private readonly Func<string?> _googleDriveRootFolderId;

    public CloudModule(
        string tokenCacheDirectory,
        Func<string?> oneDriveRootFolderId,
        Func<string?> googleDriveRootFolderId)
    {
        _tokenCacheDirectory = tokenCacheDirectory;
        _oneDriveRootFolderId = oneDriveRootFolderId;
        _googleDriveRootFolderId = googleDriveRootFolderId;
    }

    protected override void Load(ContainerBuilder builder)
    {
        RegisterOneDrive(builder);

        // Google's token store uses DPAPI (Windows-only). On other desktop OSes the provider is simply
        // not registered, so the routing backend treats Google Drive as unavailable there.
        if (OperatingSystem.IsWindows())
            RegisterGoogleDrive(builder);
    }

    private void RegisterOneDrive(ContainerBuilder builder)
    {
        var clientId = ResolveClientId("COLLECTARY_ONEDRIVE_CLIENT_ID", CloudClientIds.OneDrive);

        builder.Register(_ => new MsalAuthClient(clientId, _tokenCacheDirectory))
            .Keyed<ICloudAuthClient>(CloudProvider.OneDrive)
            .SingleInstance();

        builder.Register(c =>
        {
            var auth = c.ResolveKeyed<ICloudAuthClient>(CloudProvider.OneDrive);
            var authProvider = new BaseBearerTokenAuthenticationProvider(new GraphAccessTokenProvider(auth));
            var graph = new GraphServiceClient(authProvider);
            return new OneDriveCloudFileStore(graph, auth, _oneDriveRootFolderId);
        })
            .Keyed<ICloudFileStore>(CloudProvider.OneDrive)
            .Keyed<ICloudRootProvider>(CloudProvider.OneDrive)
            .SingleInstance();

        builder.Register(c => new CloudSyncBackend(c.ResolveKeyed<ICloudFileStore>(CloudProvider.OneDrive)))
            .Keyed<ISyncBackend>(CloudProvider.OneDrive)
            .SingleInstance();
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void RegisterGoogleDrive(ContainerBuilder builder)
    {
        var clientId = ResolveClientId("COLLECTARY_GOOGLE_CLIENT_ID", CloudClientIds.GoogleDrive);
        var clientSecret = ResolveClientId("COLLECTARY_GOOGLE_CLIENT_SECRET", CloudClientIds.GoogleDriveSecret);
        var dataStore = new DpapiDataStore(new DpapiSecretStore(Path.Combine(_tokenCacheDirectory, "google")));

        builder.Register(_ => new GoogleAuthClient(clientId, clientSecret, dataStore))
            .Keyed<ICloudAuthClient>(CloudProvider.GoogleDrive)
            .SingleInstance();

        builder.Register(c =>
        {
            var auth = c.ResolveKeyed<ICloudAuthClient>(CloudProvider.GoogleDrive);
            var drive = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = new GoogleTokenInitializer(auth),
                ApplicationName = "Collectary",
            });
            return new GoogleDriveCloudFileStore(drive, auth, _googleDriveRootFolderId);
        })
            .Keyed<ICloudFileStore>(CloudProvider.GoogleDrive)
            .Keyed<ICloudRootProvider>(CloudProvider.GoogleDrive)
            .SingleInstance();

        builder.Register(c => new CloudSyncBackend(c.ResolveKeyed<ICloudFileStore>(CloudProvider.GoogleDrive)))
            .Keyed<ISyncBackend>(CloudProvider.GoogleDrive)
            .SingleInstance();
    }

    // Prefer an environment variable so testers/CI can supply credentials without editing source;
    // fall back to the shipped placeholder otherwise.
    private static string ResolveClientId(string environmentVariable, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
