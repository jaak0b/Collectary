using Autofac;
using Collectary.Core.Ports;

namespace Collectary.UI.Desktop.Audio;

public sealed class DesktopAudioModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<NAudioRecorder>().As<IAudioRecorder>().SingleInstance();
        builder.RegisterType<NAudioPlayer>().As<IAudioPlayer>().SingleInstance();
    }
}
