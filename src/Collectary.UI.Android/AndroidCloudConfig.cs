using System;

namespace Collectary.UI.Android;

/// <summary>
/// Android-specific OneDrive sign-in configuration. The <see cref="SignatureHash"/> is the URL-safe,
/// base64 SHA-1 of the APK signing certificate; it must match the <c>msauth://</c> redirect
/// registered for the Android platform in the Azure app registration <em>and</em> the BrowserTabActivity
/// intent-filter in <c>AndroidManifest.xml</c>. MSAL prints the expected value in the error message on
/// the first sign-in attempt with a wrong hash. Ships as a placeholder; supply the real value via the
/// <c>COLLECTARY_ANDROID_SIGNATURE_HASH</c> environment variable at build time, or replace the constant.
/// </summary>
public class AndroidCloudConfig
{
    public const string PackageName = "com.collectary.app";
    public const string SignatureHashPlaceholder = "<ANDROID_SIGNATURE_HASH>";

    public string SignatureHash
    {
        get
        {
            var value = Environment.GetEnvironmentVariable("COLLECTARY_ANDROID_SIGNATURE_HASH");
            return string.IsNullOrWhiteSpace(value) ? SignatureHashPlaceholder : value;
        }
    }
}
