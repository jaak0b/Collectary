using Autofac;
using Collectary.Core.Auth;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;

namespace Collectary.UI.DI;

public class SecurityModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<UserSession>().AsSelf().As<ICurrentUser>().SingleInstance();
        builder.RegisterType<ProfileService>().As<IProfileService>().SingleInstance();
    }
}
