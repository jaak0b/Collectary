using Autofac;
using Collectary.Core.Auth;
using Collectary.Core.Ports;
using Collectary.Core.UseCases;
using Collectary.Infrastructure.Security;

namespace Collectary.UI.DI;

public class SecurityModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<Pbkdf2CredentialHasher>().As<ICredentialHasher>().SingleInstance();
        builder.RegisterType<UserSession>().AsSelf().As<ICurrentUser>().SingleInstance();
        builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();
    }
}
