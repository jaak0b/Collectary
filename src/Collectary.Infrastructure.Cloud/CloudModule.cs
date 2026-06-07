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
    private readonly MsalPlatformOptions _oneDriveMsalOptions;
    private readonly Func<string?> _oneDriveRootFolderId;
    private readonly Func<string?> _googleDriveRootFolderId;
    private readonly string? _oneDriveClientId;
    private readonly ClientIdResolver _clientIds = new();

    public CloudModule(
        string tokenCacheDirectory,
        MsalPlatformOptions oneDriveMsalOptions,
        Func<string?> oneDriveRootFolderId,
        Func<string?> googleDriveRootFolderId,
        string? oneDriveClientId = null)
    {
        _tokenCacheDirectory = tokenCacheDirectory;
        _oneDriveMsalOptions = oneDriveMsalOptions;
        _oneDriveRootFolderId = oneDriveRootFolderId;
        _googleDriveRootFolderId = googleDriveRootFolderId;
        _oneDriveClientId = oneDriveClientId;
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
        var clientId = string.IsNullOrWhiteSpace(_oneDriveClientId)
            ? _clientIds.Resolve("COLLECTARY_ONEDRIVE_CLIENT_ID", CloudClientIds.OneDrive)
            : _oneDriveClientId;

        builder.Register(_ => new MsalAuthClient(clientId, _oneDriveMsalOptions))
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
        var clientId = _clientIds.Resolve("COLLECTARY_GOOGLE_CLIENT_ID", CloudClientIds.GoogleDrive);
        var clientSecret = _clientIds.Resolve("COLLECTARY_GOOGLE_CLIENT_SECRET", CloudClientIds.GoogleDriveSecret);
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
}
