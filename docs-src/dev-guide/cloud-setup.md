# Cloud Sync Setup

Collectary can sync to **OneDrive** and **Google Drive**. Neither works out of the box, because
each needs OAuth credentials that are tied to *your* developer accounts — they're deliberately not
checked into the repo. This page walks you through getting those credentials and handing them to the
app. Budget about 20–30 minutes the first time; afterwards it's a one-off.

You only need to set up the provider(s) you actually want to test. If you just want OneDrive, skip
the Google section entirely (and vice-versa).

!!! info "What you'll end up with"
    Four values, fed to the app as environment variables:

    | Variable | Provider | Platforms |
    |---|---|---|
    | `COLLECTARY_ONEDRIVE_CLIENT_ID` | OneDrive | desktop + Android |
    | `COLLECTARY_ANDROID_SIGNATURE_HASH` | OneDrive | Android **release** |
    | `COLLECTARY_GOOGLE_CLIENT_ID` | Google Drive | Windows desktop |
    | `COLLECTARY_GOOGLE_CLIENT_SECRET` | Google Drive | Windows desktop |

    The last step of this page shows how to store them. Don't worry about them until then.

    One more is **optional** — only if you want OneDrive in the side-by-side *debug* Android build:
    `COLLECTARY_ANDROID_DEBUG_SIGNATURE_HASH` (see [Letting the debug build sign in to
    OneDrive](#letting-the-debug-build-sign-in-to-onedrive)).

---

## OneDrive

OneDrive sign-in goes through Microsoft Entra (the service formerly called Azure AD). You register
the app once, tell it who's allowed to sign in and where to send them back, and copy out an ID.

### 1. Create the app registration

1. Go to the [Microsoft Entra admin center](https://entra.microsoft.com) → **App registrations** →
   **New registration**.
2. **Name** it anything (e.g. `Collectary Dev`).
3. **Supported account types** → choose **Personal Microsoft accounts only**. This matters: the app
   asks for consumer OneDrive, so it targets personal accounts, not work/school tenants.
4. Leave the redirect URI blank for now and click **Register**.
5. On the overview page, copy the **Application (client) ID** — that's your
   `COLLECTARY_ONEDRIVE_CLIENT_ID`.

### 2. Allow the desktop (loopback) sign-in

1. Open **Authentication** → **Add a platform** → **Mobile and desktop applications**.
2. Add the redirect URI `http://localhost`. The desktop app spins up a temporary local listener on
   that address to catch the sign-in response.
3. Scroll down to **Advanced settings** → set **Allow public client flows** to **Yes**. The app is a
   public PKCE client with no secret, so this has to be on.
4. **Save**.

### 3. Allow the Android sign-in

Still under **Authentication** → **Add a platform** → **Android**:

1. **Package name**: `com.collectary.app` (the installed release id).
2. **Signature hash**: the Base64 fingerprint of the certificate that signs your release APK (the next
   section shows how to get it).
3. Entra builds a `msauth://com.collectary.app/<hash>` redirect for you. **Save**.

You can add more than one Android entry, and they happily coexist. The local *debug* build installs
under a **different** package id (`com.collectary.app.debug`) signed with a **different** key, so it
needs its own Android entry — see [Letting the debug build sign in to
OneDrive](#letting-the-debug-build-sign-in-to-onedrive) below.

### 4. Grant the Graph permissions

1. Open **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions**.
2. Add **`Files.ReadWrite`** and **`User.Read`**.
3. No admin consent is needed — each user consents when they sign in.

That's the OneDrive registration done.

### Getting the Android signature hash

The signature hash is `Base64( SHA-1( signing-certificate ) )`. It's computed from whichever key
signs the app, so each build flavour has its own:

=== "Local dev (debug build)"

    Debug builds are signed with your local Android debug keystore. Compute its hash with `keytool`
    (ships with the JDK) and `openssl`:

    ```bash
    keytool -exportcert -alias androiddebugkey \
      -keystore "$env:USERPROFILE/.android/debug.keystore" -storepass android \
      | openssl sha1 -binary | openssl base64
    ```

    The one-line Base64 string it prints is the debug hash. Because the debug build installs under its
    own package id, register it under a **separate** Android entry and feed it to the build as
    `COLLECTARY_ANDROID_DEBUG_SIGNATURE_HASH` — both covered in [Letting the debug build sign in to
    OneDrive](#letting-the-debug-build-sign-in-to-onedrive).

=== "Self-signed release"

    Generate a release keystore **once** and guard it for life — it's your app's permanent identity.
    Every published release must be signed with this same key, or phones refuse the in-place update and
    fall back to "uninstall the old version first":

    ```bash
    keytool -genkeypair -v -keystore collectary-release.keystore \
      -alias collectary -keyalg RSA -keysize 4096 -validity 36500
    ```

    Then take its hash the same way (swap in the release keystore + alias) and register that hash in
    Entra alongside your debug one — both coexist, so local debug builds and releases both sign in.

    The build server signs with this key automatically from two GitHub Actions secrets: store the
    base64 of the keystore file (`[Convert]::ToBase64String([IO.File]::ReadAllBytes('collectary-release.keystore'))`)
    in `COLLECTARY_ANDROID_KEYSTORE_BASE64`, and its password in `COLLECTARY_ANDROID_KEYSTORE_PASSWORD`.
    The `Release` target decodes the keystore, signs the APK, and discards the temporary copy. Keep the
    keystore file and password backed up outside the repo — if you ever lose both, no future build can
    update an installed app, ever.

=== "Google Play App Signing"

    If you publish through Play with App Signing, **Google holds the final signing key** and re-signs
    your upload. The hash users' phones see is Google's, so use the one from
    **Play Console → Test and release → App integrity → App signing**. Play shows it as a hex SHA-1;
    download the certificate there and run `openssl sha1 -binary | openssl base64` on it (or convert
    the hex) to get the Base64 form Entra wants.

!!! tip "Lost? Let MSAL tell you"
    If the hash is ever wrong, sign-in fails and MSAL's error message prints the exact hash it
    expected for the installed app. You can read it straight from there.

### Letting the debug build sign in to OneDrive

The debug Android build installs side by side with the release app under a different id
(`com.collectary.app.debug`) and is signed with your **local debug keystore**, so its OneDrive
redirect differs from the release one on both halves — host *and* signature hash. It's optional: skip
this and the debug app simply can't reach OneDrive; everything else works.

To turn it on:

1. **Register a second Android platform entry** in Entra (**Authentication** → **Add a platform** →
   **Android**, or **Add URI** under the existing Android platform):
    - **Package name**: `com.collectary.app.debug`
    - **Signature hash**: your debug keystore's hash (the *Local dev* tab above shows how to compute it).

    Entra builds a `msauth://com.collectary.app.debug/<hash>` redirect. It sits happily alongside the
    release `com.collectary.app` entry. **Save**.
2. **Set `COLLECTARY_ANDROID_DEBUG_SIGNATURE_HASH`** to that same debug hash. The debug build bakes it
   in (release builds keep using `COLLECTARY_ANDROID_SIGNATURE_HASH`); the app's manifest already
   lists both redirect hosts, so nothing else changes.
3. Rebuild and redeploy the debug app (`.\build.ps1 --target DeployAndroid`) and sign in.

If you skip step 2, the debug build just bakes an empty hash and OneDrive sign-in fails with a clear
MSAL error — it never breaks the build.

---

## Google Drive

Google Drive sign-in goes through a Google Cloud project. (Heads up: Google Drive currently runs on
the **Windows desktop only** — its token store uses Windows DPAPI — so there's no Android step here.)

### 1. Create a project and enable the API

1. In the [Google Cloud Console](https://console.cloud.google.com), create a new project (or reuse
   one).
2. **APIs & Services** → **Library** → search **Google Drive API** → **Enable**.

### 2. Configure the consent screen

1. **APIs & Services** → **OAuth consent screen**.
2. **User type**: **External**.
3. Fill in the app name and a support email.
4. Add the Google Drive scope (Drive file access).
5. While the app is in **Testing** status, add your own Google account under **Test users** —
   otherwise Google blocks the sign-in.

### 3. Create the OAuth client

1. **APIs & Services** → **Credentials** → **Create credentials** → **OAuth client ID**.
2. **Application type**: **Desktop app**.
3. Create it, then copy the **Client ID** and **Client secret** — those are your
   `COLLECTARY_GOOGLE_CLIENT_ID` and `COLLECTARY_GOOGLE_CLIENT_SECRET`.

!!! note "The 'secret' isn't really secret"
    Google issues installed/desktop apps a client secret, but it ships inside the app and isn't
    treated as confidential. Don't lose sleep over it the way you would a server secret.

---

## Telling Collectary about your credentials

Once you have the values, store them as environment variables. The easiest way is the interactive
NUKE target, which prompts for each one with the input masked:

```powershell
.\build.ps1 --target SetCredentials
```

It asks for each variable in turn, hides what you type (shown as `*`), re-asks if you accidentally
enter nothing (a failed paste, say), and persists each as a **per-user** Windows environment variable
— permanent across reboots, scoped to your account, never machine-wide.

Prefer to set them yourself? Any normal environment-variable mechanism works. Then sanity-check with:

```powershell
.\build.ps1 --target CheckCredentials
```

which reports each variable as `ok` or `MISSING` and fails if any is absent.

!!! warning "Set them where the build can see them"
    A Rider (or other IDE) **run** configuration injects environment variables only into that run.
    They're invisible to the build and are never delivered to an Android device. Set them as
    system/user environment variables — or in the IDE's build environment — so both the desktop run
    and the Android build pick them up. See [Building & Running](building.md#cloud-credentials) for
    the why.

---

## Verifying it works

=== "Desktop"

    ```powershell
    .\build.ps1 --target RunDesktop
    ```

    Open **Settings → Sync**, connect OneDrive (or Google Drive), and complete the sign-in in the
    browser window that appears.

=== "Android"

    ```powershell
    .\build.ps1 --target DeployAndroid
    ```

    Open **Settings → Sync** and connect OneDrive. The sign-in opens in a Chrome Custom Tab and
    returns to the app when you're done.

---

## Troubleshooting

**"Sorry, we're having trouble signing you in" (in the browser).**
Microsoft rejected the request before redirecting back. Usual causes: the client ID doesn't match a
real registration, the account type isn't set to *personal Microsoft accounts*, or — on Android — the
signature hash registered in Entra doesn't match the key that signed the installed APK. Double-check
all three line up.

**The Android app shows the redirect but never comes back.**
The `msauth://…` redirect isn't being caught. Confirm the Android platform's package name and hash in
Entra match the build, and that the same hash is in the app's manifest redirect.

**Google sign-in is blocked / "app not verified".**
While your consent screen is in *Testing*, only accounts listed under **Test users** can sign in. Add
your account there.

**Nothing happens on desktop / "public client" error.**
Make sure **Allow public client flows** is set to **Yes** in the Entra app's Authentication settings.
