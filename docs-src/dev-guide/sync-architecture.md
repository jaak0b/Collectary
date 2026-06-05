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

Presets (collections), items, and system fields, plus the image **blobs** they reference.

## A sync run

`SyncService.SyncAsync()`:

1. Lists local and remote state.
2. **Pushes** locally-changed (dirty) entities to the backend and **pulls** remotely-changed ones.
3. Reconciles using **revision numbers** and timestamps. Each syncable entity carries
   `Revision`, `BaseRevision`, `IsDirty`, `IsDeleted`/`DeletedAt`.
4. Returns a `SyncResult` reporting how many records were pushed and pulled, plus any conflicts.

## The file-system backend

`FileSystemSyncBackend` stores each entity as a per-revision JSON file (e.g.
`{id}.{revision}.json`) in kind-specific subdirectories (`presets/`, `items/`, `systemfields/`),
and images under an `images/` directory. The root directory is configurable via app preferences —
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
