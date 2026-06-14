using System.Reflection;

namespace Collectary.Presentation.Services;

public sealed class AssemblyAppVersionProvider
{
    public string Display { get; }

    public AssemblyAppVersionProvider()
        : this(typeof(AssemblyAppVersionProvider).Assembly)
    {
    }

    public AssemblyAppVersionProvider(Assembly assembly)
    {
        var informational = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Display = new AppVersion(informational).Display;
    }
}
