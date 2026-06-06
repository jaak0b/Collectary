# Sync

Sync keeps your collections, items, shared fields, and attached images in step across devices by
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
as per-revision JSON files (presets, items, shared fields) plus image blobs.

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

A deletion only ever travels as a tombstone. If the shared folder is simply *missing* an item — say
you pointed sync at a fresh or different folder, or restored your database from a backup — Collectary
treats that as "nothing to pull here," **not** as a deletion, so your local collection is never wiped
by an empty or unfamiliar folder. An image attached to a deleted item is likewise kept for as long as
its tombstone lives, so undoing a deletion (or a device that hasn't caught up yet) still finds the
picture intact.

## Editing the same collection on two devices

Renaming a collection, reordering its fields, or tweaking it on one device never disturbs the items
and values you entered on another — those edits are merged field by field rather than replacing the
whole collection, so a label change on your phone can't erase the data on your laptop. Only when the
*same* record was changed on both sides since the last sync do you get a [conflict](#resolving-conflicts)
to resolve.

## Where sync works

Sync runs on the **desktop** app (shared folder, OneDrive, or Google Drive) and on **Android**
(shared folder or OneDrive). The in-browser [demo](../demo.md) has no persistent filesystem, so sync
isn't meaningful there.

!!! note "Signing in to OneDrive on Android"
    The first time you choose OneDrive on Android, sign-in opens a secure browser tab and returns to
    Collectary automatically. Your sign-in is stored in the device's encrypted keystore, so you stay
    signed in between launches.
