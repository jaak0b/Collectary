using Autofac;
using Collectary.Core.Ports;

namespace Collectary.UI.Android.Permissions;

public sealed class AndroidPermissionsModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AndroidRuntimePermissions>().As<IRuntimePermissions>().SingleInstance();
    }
}
