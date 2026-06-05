using System.Runtime.Versioning;
using Autofac;
using Collectary.Core.Domain;
using Collectary.Core.Ports;
using Collectary.Infrastructure.Cloud;
using Collectary.Infrastructure.Cloud.Auth;

namespace Collectary.Infrastructure.Tests.Cloud;

/// <summary>
/// <see cref="CloudModule"/> wires the OneDrive provider on every platform and additionally the
/// Google Drive provider on Windows (its token store is DPAPI-backed). Each provider exposes the
/// same keyed quartet of cloud ports, and every registration resolves into a live instance.
/// </summary>
[TestFixture]
public class CloudModuleTest
{
    // A valid GUID keeps MSAL's PublicClientApplicationBuilder happy when the OneDrive auth resolves.
    private const string ClientIdGuid = "11111111-1111-1111-1111-111111111111";

    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable("COLLECTARY_ONEDRIVE_CLIENT_ID", ClientIdGuid);
        Environment.SetEnvironmentVariable("COLLECTARY_GOOGLE_CLIENT_ID", ClientIdGuid);
        Environment.SetEnvironmentVariable("COLLECTARY_GOOGLE_CLIENT_SECRET", "secret");
    }

    [TearDown]
    public void TearDown()
    {
        Environment.SetEnvironmentVariable("COLLECTARY_ONEDRIVE_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("COLLECTARY_GOOGLE_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("COLLECTARY_GOOGLE_CLIENT_SECRET", null);
    }

    private static IContainer BuildContainer()
    {
        var options = new AndroidMsalPlatformOptionsFactory("com.collectary.app", "hash", () => null).Create();
        var builder = new ContainerBuilder();
        builder.RegisterModule(new CloudModule("cache-dir", options, () => "one-root", () => "google-root"));
        return builder.Build();
    }

    [Test]
    public void RegistersOneDriveSyncBackend() =>
        Assert.That(BuildContainer().IsRegisteredWithKey<ISyncBackend>(CloudProvider.OneDrive), Is.True);

    [Test]
    public void ResolvesOneDrivePorts()
    {
        var container = BuildContainer();

        Assert.Multiple(() =>
        {
            Assert.That(container.ResolveKeyed<ICloudAuthClient>(CloudProvider.OneDrive), Is.Not.Null);
            Assert.That(container.ResolveKeyed<ICloudFileStore>(CloudProvider.OneDrive), Is.Not.Null);
            Assert.That(container.ResolveKeyed<ICloudRootProvider>(CloudProvider.OneDrive), Is.Not.Null);
            Assert.That(container.ResolveKeyed<ISyncBackend>(CloudProvider.OneDrive), Is.Not.Null);
        });
    }

    [Test]
    [Platform("Win")]
    [SupportedOSPlatform("windows")]
    public void OnWindows_RegistersAndResolvesGoogleDrivePorts()
    {
        var container = BuildContainer();

        Assert.Multiple(() =>
        {
            Assert.That(container.IsRegisteredWithKey<ISyncBackend>(CloudProvider.GoogleDrive), Is.True);
            Assert.That(container.ResolveKeyed<ICloudAuthClient>(CloudProvider.GoogleDrive), Is.Not.Null);
            Assert.That(container.ResolveKeyed<ICloudFileStore>(CloudProvider.GoogleDrive), Is.Not.Null);
            Assert.That(container.ResolveKeyed<ISyncBackend>(CloudProvider.GoogleDrive), Is.Not.Null);
        });
    }
}
