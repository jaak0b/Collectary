using Autofac;
using Collectary.Core.Ports;

namespace Collectary.UI.Desktop.Camera;

public sealed class DesktopCameraModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<DesktopLiveCamera>().As<ILiveCamera>().SingleInstance();
    }
}
