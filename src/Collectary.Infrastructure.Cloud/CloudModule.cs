using Autofac;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Cloud.Auth;
using Collectary.Infrastructure.Cloud.OneDrive;
using Collectary.Infrastructure.Sync;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;

namespace Collectary.Infrastructure.Cloud;

/// <summary>
/// Desktop-only DI for cloud sync providers. Registered by <c>Collectary.UI.Desktop</c> (never by the
/// Browser build), so the MSAL/Graph SDKs stay out of the WebAssembly graph. Each provider exposes a
/// keyed <see cref="ICloudAuthClient"/>, <see cref="ICloudFileStore"/>/<see cref="ICloudFolderProvisioner"/>,
/// and an <see cref="ISyncBackend"/> that the routing backend selects when that provider is active.
/// </summary>
public class CloudModule : Module
{
    private readonly string _tokenCacheDirectory;
    private readonly Func<string?> _oneDriveRootFolderId;

    public CloudModule(string tokenCacheDirectory, Func<string?> oneDriveRootFolderId)
    {
        _tokenCacheDirectory = tokenCacheDirectory;
        _oneDriveRootFolderId = oneDriveRootFolderId;
    }

    protected override void Load(ContainerBuilder builder)
    {
        // Prefer an environment variable so testers/CI can supply the registered client id without
        // editing source; fall back to the shipped placeholder otherwise.
        var oneDriveClientId = Environment.GetEnvironmentVariable("COLLECTARY_ONEDRIVE_CLIENT_ID");
        if (string.IsNullOrWhiteSpace(oneDriveClientId))
            oneDriveClientId = CloudClientIds.OneDrive;

        builder.Register(_ => new MsalAuthClient(oneDriveClientId, _tokenCacheDirectory))
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
}
