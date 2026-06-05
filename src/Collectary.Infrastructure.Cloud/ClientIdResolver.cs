namespace Collectary.Infrastructure.Cloud;

/// <summary>
/// Resolves an OAuth client id/secret: prefers an environment variable so testers/CI can supply
/// real credentials without editing source, and falls back to the shipped placeholder otherwise.
/// </summary>
public class ClientIdResolver
{
    public string Resolve(string environmentVariable, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
