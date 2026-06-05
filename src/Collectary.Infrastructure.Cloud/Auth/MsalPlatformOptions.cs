using Microsoft.Identity.Client;

namespace Collectary.Infrastructure.Cloud.Auth;

/// <summary>
/// The platform-specific knobs OneDrive's MSAL sign-in needs, so <see cref="MsalAuthClient"/> stays
/// platform-agnostic and each app head (desktop, Android) supplies its own values:
/// <list type="bullet">
/// <item><see cref="RedirectUri"/> — desktop uses the loopback <c>http://localhost</c>; Android a
/// custom <c>msauth://{package}/{hash}</c> scheme caught by the manifest's BrowserTabActivity.</item>
/// <item><see cref="ParentActivityProvider"/> — null on desktop; on Android returns the current
/// <c>Activity</c> MSAL needs to launch the Chrome Custom Tab.</item>
/// <item><see cref="ConfigureTokenCache"/> — desktop wires the DPAPI-backed <c>MsalCacheHelper</c>;
/// Android leaves it null so MSAL's built-in Keystore-backed cache is used.</item>
/// </list>
/// </summary>
public sealed record MsalPlatformOptions(
    string RedirectUri,
    Func<object?>? ParentActivityProvider,
    Action<IPublicClientApplication>? ConfigureTokenCache);
