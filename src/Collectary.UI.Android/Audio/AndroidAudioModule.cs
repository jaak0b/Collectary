using Autofac;
using Collectary.Core.Ports;

namespace Collectary.UI.Android.Audio;

public sealed class AndroidAudioModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<AndroidAudioRecorder>().As<IAudioRecorder>().SingleInstance();
        builder.RegisterType<AndroidAudioPlayer>().As<IAudioPlayer>().SingleInstance();
    }
}
