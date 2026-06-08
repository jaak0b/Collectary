# Sync Architecture

This page describes how [sync](../user-guide/sync.md) works under the hood. Sync is defined entirely
in terms of Core **ports**, with concrete adapters in Infrastructure.

## The pieces

| Port (Core) | Role | Adapter (Infrastructure) |
|---|---|---|
| `ISyncService` | Orchestrates a sync: reconciles local + remote, returns a result, resolves conflicts. | `SyncService` |
| `ISyncBackend` | The remote store abstraction — list/read/write/delete for entities, plus blob (image) operations. | `FileSystemSyncBackend` |
| `ISyncStore` | The local store — loads entities, reconciles revisions/timestamps, handles merges and tombstones. | `EfSyncStore` |
| `ISyncSerializer` | JSON (de)serialization, including polymorphic `FieldDefinition` handling. | uses a polymorphic field resolver |
| `ISyncStatus` | Tracks sync-related state/preferences. | backed by app preferences |

## What syncs

Presets (collections), items, shared fields, **user profiles**, and **collection shares** — plus the
image **blobs** they reference.

### Why profiles and shares sync

Collection visibility is owner-gated: a preset is only shown to the profile that owns it (or a profile
it has been shared with). Each install mints its own random profile ids, so if only presets and items
synced, another device would receive collections owned by a profile it has never heard of — the data
would import correctly but stay invisible behind the owner filter.

Syncing the `User` (profile) and `CollectionShare` records too closes that gap. After two installs sync
the same folder, each one's profiles appear in the other's profile picker; switch to a synced-in
profile to see its collections, and a shared collection shows up for the share target without switching
at all. Profiles carry **no secrets** — passwords and email were removed from the model — so replicating
them to a shared folder is safe.

Because shares are syncable, **revoking** a share is a soft-delete (a tombstone), not a hard delete —
otherwise the revoked share would simply re-download from a peer on the next sync and silently restore
access. Re-granting a previously-revoked share revives the same tombstoned row in place, so the
`(preset, user)` uniqueness still holds. The same applies to a profile.

Profiles are reconciled **before** presets and items, so an owner always exists locally before the
collections that reference it arrive. Usernames have a unique, case-insensitive index; when an incoming
profile's username collides with a *different* local profile, the import keeps the incoming id and
display name but uniquifies the stored username, so ownership references still resolve and both profiles
coexist. Two installs that each happen to have an "Alice" profile therefore stay two distinct profiles —
identities are never merged.

## Adding a sync kind

The kinds are **table-driven**, not switch-driven — there is no per-kind `switch` to keep in sync. The
single source of truth for orchestration is `SyncKindCatalog.Describe(...)`, which yields one `SyncKind`
descriptor per kind (wire string, how to load locals, label, serialize, deserialize, apply), in
dependency order (owners first). Both `SyncService` and `BackupService` loop over that catalog, so a new
kind is backed up, restored, and synced from one place. The persistence side mirrors this with a single
`EfSyncStore` ops map (`kind → find/delete/purge`, built from a generic `OpsFor<T>()`). The conflict
dialog resolves its label by convention (`Sync_Kind{kind}`). Completeness-guard tests fail if a new
`SyncEntityKind` value is added without its catalog and ops-map rows.

## A sync run

`SyncService.SyncAsync()`:

1. Lists local and remote state.
2. **Pushes** locally-changed (dirty) entities to the backend and **pulls** remotely-changed ones.
3. Reconciles using **revision numbers** and timestamps. Each syncable entity carries
   `Revision`, `BaseRevision`, `IsDirty`, `IsDeleted`/`DeletedAt`.
4. Returns a `SyncResult` reporting how many records were pushed and pulled, plus any conflicts.

## The file-system backend

`FileSystemSyncBackend` stores each entity as a per-revision JSON file (e.g.
`{id}.{revision}.json`) in kind-specific subdirectories (`presets/`, `items/`, `sharedfields/`,
`users/`, `shares/`), and images under an `images/` directory. The backend and serializer are
kind-agnostic, so adding a new syncable kind needs no backend change. The root directory is configurable via app preferences —
point multiple devices at the same shared folder (a cloud-drive folder, network share, etc.).

## Conflicts

A conflict arises when an entity was modified **both** locally and remotely since the last common
revision. `SyncService` surfaces these and the user resolves each by keeping local or taking
remote (`ResolveAsync(conflict, keepLocal)`); the resolution is written out on the next sync.

## Deletions & tombstones

Deletions are tracked as **tombstones** so they propagate instead of resurrecting on the next pull.
`EfSyncStore` retains tombstones for a configurable number of days
(`TombstoneRetentionDays`, default 30) and then prunes them.

## Polymorphic serialization

Because a collection's fields are a polymorphic `FieldDefinition` hierarchy, the serializer uses a
polymorphic resolver so every field type round-trips through JSON correctly. A newly
[added field type](adding-a-field-type.md) is handled by the same resolver with no special-casing.

## Cloud providers (OneDrive / Google Drive)

Beyond a plain shared folder, sync can talk to a cloud provider's API directly. These live in
`Collectary.Infrastructure.Cloud` and plug in behind the same `ISyncBackend` port via a small set of
extra ports:

| Port (Core) | Role |
|---|---|
| `ICloudAuthClient` | Per-provider OAuth — sign in/out, hand back an access token, expose the account label. |
| `ICloudFileStore` | Per-provider file/folder CRUD; `CloudSyncBackend` adapts it to the `{id}.{revision}.json` layout. |
| `ICloudRootProvider` | The starting folder for the folder picker. |

`RoutingSyncBackend` picks the active provider at runtime from app preferences, so the rest of the
app never branches on which cloud you chose. Each head registers the providers it supports through a
`CloudModule` set on `App.PlatformModules` — the Browser build registers none, keeping the cloud
SDKs out of the WebAssembly graph.

### MSAL platform options (OneDrive)

OneDrive auth (`MsalAuthClient`) is the same code on every platform, but three things differ per
platform and are supplied through `MsalPlatformOptions`:

| Knob | Desktop | Android |
|---|---|---|
| Redirect URI | loopback `http://localhost` (system browser) | `msauth://com.collectary.app/<hash>` (Chrome Custom Tab) |
| Interactive parent | none | the current `Activity` |
| Token cache | DPAPI-backed `MsalCacheHelper` | MSAL's built-in Android Keystore cache (none configured) |

Each head builds the options with a factory — `DesktopMsalPlatformOptionsFactory` or
`AndroidMsalPlatformOptionsFactory` — and passes them into `CloudModule`. The Cloud library stays
`net8.0`; because the Android head targets `net10.0-android`, NuGet still ships MSAL's
`net8.0-android` runtime asset (Custom Tabs + Keystore), so no multi-targeting is needed.

MSAL's Custom Tabs flow launches its own `AuthenticationActivity`, which loads the AndroidX Browser
binding the moment that activity resumes. That binding is **not** dragged in transitively, so the
Android head references `Xamarin.AndroidX.Browser` explicitly — without it, tapping **Connect** for
OneDrive crashes with a `FileNotFoundException` for `Xamarin.AndroidX.Browser` inside
`AuthenticationActivity.onResume`. `AndroidMsalBrowserDependencyTest` guards the reference so it can't
be dropped again.

Google Drive remains desktop-only for now: its sign-in uses a loopback HTTP listener and a
DPAPI-encrypted token store, both of which need reworking before it can run on mobile.

### Setting up OneDrive on Android

For the full, step-by-step credential walkthrough (both OneDrive and Google Drive, from registration
to running the app), see [Cloud Sync Setup](cloud-setup.md). The short version, Android-specific:

Two one-time steps outside the code, because they depend on your signing certificate and app
registration:

1. **Azure app registration** — add an **Android** platform with package name `com.collectary.app`
   and the **signature hash** of your APK signing key. MSAL prints the expected hash in the error
   message on the first sign-in attempt if it's wrong.
2. **Manifest + build** — put that same hash into the `BrowserTabActivity` `android:path` in
   `AndroidManifest.xml`, and supply it to the redirect via the `COLLECTARY_ANDROID_SIGNATURE_HASH`
   environment variable at build time (it defaults to a placeholder otherwise).

## Scheduling

On the desktop, a dispatcher-based scheduler runs auto-sync on the configured interval (default 5
minutes; `0` disables it).

## Cloud-provider auth hardening

The API-based cloud backends (OneDrive via MSAL, Google Drive via Google.Apis) carry a few
deliberate guardrails:

- **OAuth tokens are encrypted at rest.** MSAL uses its DPAPI-backed cache; the Google client routes
  its token store through `DpapiSecretStore` (DPAPI, `CurrentUser` scope) instead of the SDK's
  default plaintext file store.
- **Least-privilege scopes.** Google uses `drive.file`, so the app only ever sees files it created
  (an app-owned `Collectary` folder), never the user's whole drive.
- **The Google `id_token` is signature-verified.** When we read the signed-in account's email for
  display, `GoogleAuthClient` validates the token via `GoogleJsonWebSignature.ValidateAsync`
  (signature, issuer, expiry) rather than trusting an unsigned payload; on any failure it falls back
  to a generic label and trusts nothing from the token.
- **Drive folder ids are validated before they reach a query.** `GoogleDriveCloudFileStore` only
  interpolates ids matching Drive's `[A-Za-z0-9_-]` charset into its `Q` filter, so a stray quote
  can't break out of the query.
