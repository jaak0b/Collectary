# Cloud Sync — Setup & Manual Verification (OneDrive)

This guide covers the one-time developer setup needed to exercise OneDrive cloud sync, and the
manual steps to verify the end-to-end flow. (Google Drive is not implemented yet.)

## 1. Register the OneDrive (Microsoft Entra) app — one time, free

You register **one** public app; every end user signs into their *own* Microsoft account against it.

1. Go to the [Azure Portal](https://portal.azure.com) → **Microsoft Entra ID → App registrations → New registration**.
2. **Name:** e.g. `Collectary`.
3. **Supported account types:** *Personal Microsoft accounts only*. The app's MSAL authority is
   `consumers`, so only personal accounts (which always have OneDrive) can sign in — this avoids
   work/school tenants that lack a OneDrive/SharePoint license. (An existing broader registration
   also works; the app restricts to personal accounts at runtime regardless.)
4. **Redirect URI:** platform **Mobile and desktop applications**, value `http://localhost`.
5. Create it, then open **Authentication** → enable **Allow public client flows = Yes** (no client secret is used).
6. Open **API permissions** → **Add a permission → Microsoft Graph → Delegated** → add:
   - `Files.ReadWrite`
   - `User.Read`
   - `offline_access`
   (No admin consent is required for personal accounts.)
7. Copy the **Application (client) ID** from the app's **Overview** page.

> **Which id?** Use the **Application (client) ID** (in the Manifest this is `"appId"`). Do **not** use
> the **Object ID** (Manifest `"id"`) or the **Directory (tenant) ID** — they're similar-looking GUIDs.

## 2. Provide the client ID to the app

The app reads `COLLECTARY_ONEDRIVE_CLIENT_ID` first, then falls back to the constant in
[`CloudClientIds.cs`](../src/Collectary.Infrastructure.Cloud/CloudClientIds.cs). Pick whichever option
fits how you launch the app. Without a real id, **Connect** fails with `AADSTS90013: Invalid input
received from the user` (Azure rejecting the `<ONEDRIVE_CLIENT_ID>` placeholder).

> **Key gotcha:** an environment variable is only seen by processes started from the *same* place it
> was set. Setting `$env:…` in one PowerShell window does **not** affect the app if you then launch it
> by double-clicking the exe, from a different terminal, or from an IDE. Match the option below to your
> launch method.

### Option A — PowerShell, current session (quick test)
Set it and launch the exe **from that same terminal**:
```powershell
$env:COLLECTARY_ONEDRIVE_CLIENT_ID = "<your-application-client-id>"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe
```
Verify it's set in the shell before launching: `echo $env:COLLECTARY_ONEDRIVE_CLIENT_ID`.
The variable disappears when the terminal closes.

### Option B — persistent (all future terminals)
```powershell
setx COLLECTARY_ONEDRIVE_CLIENT_ID "<your-application-client-id>"
```
`setx` does **not** affect the terminal you ran it in — open a **new** terminal (or restart the
app/IDE) afterwards. Stored under your Windows user account permanently.

### Option C — launching from an IDE (JetBrains Rider / Visual Studio)
The IDE starts the process, so a variable set in an external terminal won't apply. Add it to the run
configuration instead:
- **Rider:** Run → Edit Configurations… → select **Collectary.UI.Desktop** → **Environment variables**
  → add `COLLECTARY_ONEDRIVE_CLIENT_ID=<your-application-client-id>`.
- **Visual Studio:** Debug → *Project* Debug Properties → Environment variables.

### Option D — hardcode (simplest; the id is public, not a secret)
Replace the placeholder directly in
[`CloudClientIds.cs`](../src/Collectary.Infrastructure.Cloud/CloudClientIds.cs):
```csharp
public const string OneDrive = "<your-application-client-id>";
```
Then rebuild. Works regardless of how the app is launched. (The env var still overrides this if set.)

> After changing the id by any method, do a **rebuild + relaunch** (stop any running instance first).
> To confirm it took effect, glance at the `client_id=` value in the browser sign-in URL — it should be
> your GUID, not `<ONEDRIVE_CLIENT_ID>`.

## 3. Manual verification (desktop)

```powershell
try { Get-Process -Name "Collectary.UI.Desktop" | Stop-Process -Force } catch {}
$env:COLLECTARY_ONEDRIVE_CLIENT_ID = "<your-application-client-id>"
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe
```

Then, in the app:

1. Open **Settings**.
2. Under **Storage provider**, choose **OneDrive**. (Status shows *Not connected*.)
3. Click **Connect…** → the system browser opens → sign in with a Microsoft account and consent.
   Status changes to **Connected as &lt;your email&gt;**.
4. Click **Set up sync folder** → the folder picker lists your OneDrive folders. Navigate / create a
   folder, then **Use this folder**. Status shows **Sync folder ready**.
5. Create or edit a collection item, then trigger **Sync**.
6. **Verify upload:** in OneDrive (web or the OneDrive app), open the chosen folder and confirm
   `presets/`, `items/`, `systemfields/` subfolders with `*.json` files (and `images/` if any).
7. **Verify round-trip:** on a second machine/profile (or after clearing the local DB), connect the
   same account + folder and sync — the items should download.

### Notes
- Tokens are cached locally, encrypted (DPAPI on Windows) via the MSAL cache extensions, so you stay
  signed in across launches until you click **Disconnect**.
- Files larger than 4 MB upload via a resumable Graph upload session automatically.
- The **Use installed OneDrive folder** button (Folder provider) is the no-API alternative: it points
  the plain folder backend at your locally-synced OneDrive directory, if the OneDrive desktop client
  is installed.

---

# Google Drive

Google Drive works the same way in the app (Settings → **Google Drive** → Connect → Set up sync
folder → Sync), with its own one-time registration. **Windows only** for now — the encrypted token
store uses DPAPI, so the Google provider is not registered on other desktop OSes.

## 1. Register the Google OAuth client — one time, free

1. [Google Cloud Console](https://console.cloud.google.com) → create (or pick) a project.
2. **APIs & Services → Library** → enable the **Google Drive API**.
3. **APIs & Services → OAuth consent screen** → User type **External** → fill app name + support
   email → add the scope **`.../auth/drive.file`** → save. Add yourself as a **Test user** while the
   screen is in *Testing*.
4. **APIs & Services → Credentials → Create credentials → OAuth client ID** → Application type
   **Desktop app** → create. Copy the **Client ID** *and* **Client secret**.
   > For installed/desktop apps the "secret" is **not** confidential (it ships in the app); Google's
   > flow still requires it.
5. **Publishing:** `drive.file` is a non-sensitive scope, so it avoids the restricted-scope security
   assessment. While the consent screen stays in *Testing* only added test users can connect (and
   refresh tokens expire weekly); **Publish to Production** for general use. Until brand verification,
   users see a click-through "Google hasn't verified this app" screen.

## 2. Provide the credentials to the app

Same model as OneDrive (see above), but two values — env vars win over the placeholders in
[`CloudClientIds.cs`](../src/Collectary.Infrastructure.Cloud/CloudClientIds.cs):

```powershell
$env:COLLECTARY_GOOGLE_CLIENT_ID     = "<your-google-client-id>"
$env:COLLECTARY_GOOGLE_CLIENT_SECRET = "<your-google-client-secret>"
```
(Match the launch method — same-terminal / `setx` / Rider run config / hardcode — as the OneDrive
section. Rebuild + relaunch after changing them.)

## 3. Notes & differences from OneDrive
- **Scope is `drive.file`**, so the app only sees files/folders it created. There is no browsing of
  your whole Drive — "Set up sync folder" starts at an app-owned **Collectary** folder, where you
  create/pick the sync folder.
- Tokens are stored **DPAPI-encrypted** (our custom data store), not the SDK's plaintext file store.
- Uploads use Drive's resumable upload protocol.
- Verify the same way: after Sync, the `presets/ items/ systemfields/` subfolders appear inside the
  chosen Drive folder (visible in the Google Drive web UI).
