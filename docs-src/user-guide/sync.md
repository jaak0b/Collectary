# Sync

Sync keeps your collections, items, system fields, and attached images in step across devices by
reconciling them through a **shared folder** — for example a folder kept in a cloud-drive (Dropbox,
OneDrive, a network share, …).

## Setting it up

In [Settings](settings.md) you configure:

| Setting | Meaning |
|---|---|
| **Sync location** | The folder used as the shared store. Point every device at the same folder. |
| **Auto-sync** | Whether Collectary syncs automatically on a timer. |
| **Auto-sync interval** | How often the timer runs (default: every 5 minutes; `0` disables it). |
| **Tombstone retention** | How long deletions are remembered so they propagate to other devices (default: 30 days). |

You can also trigger a sync manually at any time.

## What a sync does

Each sync **pushes** your local changes to the shared folder and **pulls** changes made elsewhere,
reconciling the two using revision numbers and timestamps. Entities are stored in the shared folder
as per-revision JSON files (presets, items, system fields) plus image blobs.

After a sync you'll see how many records were pushed and pulled, and whether any conflicts need
your attention.

## Resolving conflicts

A **conflict** happens when the same item was changed both locally and remotely since the last
sync. Collectary can't know which version you want, so it asks:

- **Keep local** — your version wins.
- **Take remote** — the other device's version wins.

Resolve each conflict and the chosen version is written everywhere on the next sync.

## Deletions

When you delete something, Collectary records a **tombstone** so the deletion propagates to other
devices instead of the item reappearing on the next pull. Tombstones are kept for the retention
period you configured, then cleaned up.

!!! note "Sync needs a real filesystem"
    Sync works on the desktop app. The in-browser [demo](../demo.md) has no persistent filesystem,
    so sync isn't meaningful there.
