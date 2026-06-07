using Autofac;
using Collectary.Core.Ports;

namespace Collectary.UI.Android.Camera;

public sealed class AndroidCameraModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AndroidLiveCamera>().As<ILiveCamera>().SingleInstance();
    }
}
