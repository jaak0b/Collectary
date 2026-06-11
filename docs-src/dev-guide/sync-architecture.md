# Sync Architecture

This page describes how [sync](../user-guide/sync.md) works under the hood. Sync is defined entirely
in terms of Core **ports**, with concrete adapters in Infrastructure.

## The pieces

| Port (Core) | Role | Adapter (Infrastructure) |
|---|---|---|
| `ISyncService` | Orchestrates a sync: writes this device's snapshot, reads the others, merges them deterministically. | `SyncService` |
| `ISyncBackend` | The remote store abstraction — list/read/write/delete for documents, plus blob (image) operations. | `FileSystemSyncBackend` |
| `ISyncStore` | The local store — loads entities, applies merged winners, records/applies tombstones, holds the Lamport high-water. | `EfSyncStore` |
| `ISyncSerializer` | JSON (de)serialization, including polymorphic `FieldDefinition` handling. | uses a polymorphic field resolver |
| `IDeviceIdentity` | A stable per-install device id (a GUID, persisted in preferences). | `PreferencesDeviceIdentity` |
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

Because shares are syncable, **revoking** a share is a hard delete that records a **tombstone** (a
permanent marker holding just the id), not a silent row removal — otherwise the revoked share would
re-download from a peer on the next sync and restore access. The tombstone makes the deletion *win*
on every device (see [Deletions](#deletions--tombstones)). Re-granting afterwards simply inserts a
fresh share row; the old row is gone, so the `(preset, user)` uniqueness still holds. The same applies
to a profile.

Profiles are reconciled **before** presets and items, so an owner always exists locally before the
collections that reference it arrive. Usernames have a unique, case-insensitive index; when an incoming
profile's username collides with a *different* local profile, the import keeps the incoming id and
display name but uniquifies the stored username, so ownership references still resolve and both profiles
coexist. Two installs that each happen to have an "Alice" profile therefore stay two distinct profiles —
identities are never merged.

## Adding a sync kind

The kinds are **table-driven**, not switch-driven — there is no per-kind `switch` to keep in sync. The
single source of truth for orchestration is `SyncKindCatalog.Describe(...)`, which yields one `SyncKind`
descriptor per kind (wire string, how to load locals, label, serialize, deserialize, apply, and how to
read/write its slot in a snapshot), in dependency order (owners first). Each descriptor is built by a
generic `For<T>(...)` factory, so the cast/serialize/deserialize boilerplate is written once rather than
copy-pasted per kind. Both `SyncService` and `BackupService` loop over that catalog, so a new kind is
backed up, restored, and synced from one place.

The persistence side mirrors this with a single `EfSyncStore` ops map
(`kind → find / findMany / deleteMany / anyDirty`, built from a generic `OpsFor<T>()`, plus an optional
per-kind **pre-delete** hook). **Hard-deletion, batched push-stamping, and the "is anything dirty?"
probe all loop that one ops map** — none of them re-enumerates the kinds — so adding a sync kind touches
exactly three places: its `SyncKindCatalog` row, its `EfSyncStore` ops-map row, and its list on
`DeviceSnapshot`. A kind-specific quirk stays local to its registration: the `Preset` self-FK re-parent
(below) rides along as that kind's pre-delete hook, not as a special case inside the shared delete loop.
Completeness-guard tests fail if a new `SyncEntityKind` value is added without its catalog and ops-map rows.

## Versioning: a Lamport clock + device id

Every syncable entity carries a **version** that is a pair: a `Lamport` number plus the
`LastModifiedByDeviceId` (a GUID) of whoever last wrote it. The Lamport number is a *logical* clock —
each device keeps one running high-water mark (`SyncState.MaxObservedLamport`) and a new edit takes
`max(seen) + 1`, so an edit made *after* seeing another device's edit always gets a strictly higher
number. Wall-clock time is never used for ordering (device clocks can't be trusted).

To compare two versions of the same entity, compare the `Lamport` first; on an exact tie, the higher
`DeviceId` wins. Because device ids are unique this is a **total order** — two devices can never mint
the same version with different content, and every device computes the same winner. This is what makes
the merge deterministic and convergent (re-syncing in any order, with any delay, reaches the same
state — no oscillation).

## A sync run

`SyncService.SyncAsync()` (per-device snapshot files, merged on read):

1. Reads **every other** device file in the folder (in parallel) and computes a small
   **fingerprint** from each peer file's `sha256:` header (the content hash already in the file — no
   re-hash, no deserialize) plus this device's tombstone count. If our own file is present in the
   listing, nothing local is dirty, and that fingerprint matches the one we stored after the last
   successful sync, the run **returns immediately** — it never deserializes the peers, loads the local
   entity graph, or rewrites its file. So an idle auto-sync tick on a large collection costs reading a
   couple of small device files and comparing a hash, not re-materializing and re-serializing the whole
   dataset. (Downloading the peer files is cheap; loading the full local graph and rewriting the
   multi-MB snapshot is the expensive part this skips. A peer that *changed* bumps its header hash; a
   local edit sets a dirty flag; a deletion changes the tombstone count — any of which forces the full
   path. The fingerprint is only re-stored when no peer file was unreadable, so a corrupt peer keeps the
   full path running and stays visible. The same applies to a peer that is *listed* but whose file cannot
   be read at all — it counts toward `UnreadableDevices`, blocks both the fast-path and the fingerprint
   store, and so keeps being reported every run instead of silently vanishing from the merge.)
2. When something did change, it deserializes the peer files (each in its own try/catch — a corrupt or
   half-written file is skipped and counted, never aborting the run), loads this device's live entities,
   and stamps any **dirty** ones with a fresh Lamport number and this device id (all stamps persisted in
   **one** batched store call), then unions every tombstone id it learns.
3. Applies the union of tombstone ids — hard-deleting locally (**delete-wins**) — **before** writing its
   own file. Only then does it write **the one file this device owns** (`devices/{deviceId}.json`),
   containing its still-live entities and **re-emitting the full learned tombstone set**. Writing after
   the delete means a device never re-advertises an entity it has just tombstoned, and the re-emission
   keeps a deletion propagating even after its originating device's file is gone.
4. Folds the remotes: every non-deleted id resolves to the highest `(Lamport, DeviceId)` winner, applied
   only if it is newer than the local copy. There is no shared mutable object — each device only ever
   writes its *own* file — so a concurrent write can never be lost.
5. Returns a `SyncResult` that makes **every** partial outcome visible instead of letting any of them
   masquerade as a clean success:
   - `Pushed` / `Pulled` — records sent and applied.
   - `Skipped` — incoming records that could not be applied locally (e.g. a transient constraint
     violation); logged and retried next run.
   - `UnreadableDevices` — peer snapshots skipped because their file could not be read, their checksum
     failed, or they would not deserialize, so the user learns a device could not be merged rather than
     seeing "all good".
   - `ImagesFailed` — referenced images that could not transfer this run; image transfer is isolated
     per blob, so one bad image is counted and retried while the rest still sync.
   - `BackendUnavailable` — the configured location was not reachable, so **nothing** synced; the UI
     shows a distinct "not reachable" notice and does **not** update the last-synced time (an
     unavailable backend must never look like a successful empty sync).

   When any of these is non-zero the UI raises a single non-blocking notice listing what needs
   attention, instead of silently reporting success. **There are no conflicts** — the merge is automatic
   and deterministic.

### Integrity & self-recovery

Each device file is written with a `sha256:<hex>` header line over its JSON body. On read, the body is
re-hashed and compared; a mismatch — or a **missing** header, since every file we write carries one, so
its absence means foreign or truncated content — marks that one file unreadable, so it is skipped (and
counted in `UnreadableDevices`) while the rest still merge. The check is strict on purpose: unverified
content is never trusted just because it parses. The local database is the source of truth and is never
bulk-cleared by sync, so a device whose own file is lost or corrupt simply regenerates it from its
database on the next run — the fingerprint fast-path checks that our own id is still in the folder
listing, so a vanished own file always forces a full run that rewrites it.

### Known scaling limits

The fingerprint fast-path makes an *idle* tick cheap, but a tick that *does* change still reloads the
full local entity graph and rewrites the device's **entire** snapshot file — even for a one-item edit,
because each device file holds that device's complete known state. For very large collections (tens to
hundreds of thousands of entries) that per-change rewrite is the real ceiling; splitting the snapshot
into per-kind or per-chunk shards (so a small edit writes a small file) is tracked as future work and
must preserve the no-shared-write-race, deterministic-merge, and transitive-propagation guarantees.

### Deleting a parent collection

A collection (`Preset`) may have sub-collections pointing at it via `ParentPresetId`, and that foreign key
is `Restrict` so the **interactive** "delete collection" guard still blocks deleting a parent that has
children. The **sync** delete path, however, must not abort when a parent's tombstone arrives before a
peer's still-live child. This is handled as the `Preset` kind's **pre-delete hook**: before the shared
delete loop removes any rows, the hook re-parents any surviving child of a to-be-deleted parent to the
root (`ParentPresetId = null`). So delete-wins propagates without the `Restrict` FK aborting the whole
run, the fix-up lives next to the `Preset` registration rather than as a special case in the generic
deletion code, and the interactive guard is left untouched.

## The file-system backend

`FileSystemSyncBackend` stores each document as one flat `{id}.json` file in a kind-specific
subdirectory; sync uses a single `devices/` directory holding one file per install (named by its
device id), plus images under `images/`. There is no per-file revision scheme — each device only ever
rewrites its *own* file, a write goes to a temp file and is moved into place atomically, and the
`sha256:` header catches a torn read on the other side, so one flat name per document is all the
layout needs. (An earlier `{id}.{revision}.json` scheme was removed as a clean break; old-format
files are ignored by readers and swept away by the owning device's next write.) The backend and
serializer are kind-agnostic. The root directory is configurable via app preferences — point multiple
devices at the same shared folder (a cloud-drive folder, network share, etc.).

## Automatic merge (no conflict dialog)

When two devices edit the **same** entity without seeing each other, the higher `(Lamport, DeviceId)`
wins automatically — there is no "keep mine / keep theirs" prompt. The losing edit is not destroyed;
it still lives in its origin device's file and is recoverable. There is no conflict-resolution call on
the port — `ISyncService` exposes only `SyncAsync()`, because the engine never raises a conflict.

## Deletions & tombstones

Deleting an object **hard-deletes its row immediately** — the user's data leaves the disk at once — and
records a tiny `Tombstone` (just the deleted id) in the same transaction. Each device file carries its
tombstone ids; the merge treats **delete as the winner** (any id with a tombstone anywhere is removed
locally, regardless of version), so a deletion can never be resurrected by a stale live copy returning
from an offline device. Because ids (GUIDs) are never reused, tombstones are kept permanently — there
is no time-based retention to guess at.

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
| `ICloudFileStore` | Per-provider file/folder CRUD; `CloudSyncBackend` adapts it to the flat `{id}.json` layout. |
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
