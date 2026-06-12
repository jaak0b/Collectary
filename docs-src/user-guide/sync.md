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

Each device writes **one file of its own** to the shared folder (named by its device id) holding the
data it knows about, then reads the other devices' files and merges them in. Because a device only ever
writes *its own* file, two devices syncing at once can never overwrite each other's changes.

The sync icon in the top bar opens a small status panel: press **Sync now** there and the panel stays
open while it works — you'll see "Syncing…" turn into how many records were pushed and pulled, plus
the updated last-sync time. There's nothing to resolve by hand — the merge is automatic (see below).

## When two devices change the same thing

You no longer get a conflict prompt. If the same item is edited on two devices before they sync,
Collectary keeps the **most recent** edit automatically and everywhere — both devices end up showing
the same value, with no dialog to dismiss. (The other edit isn't destroyed; it stays in that device's
file.) Editing *different* things — or different fields of the same item — keeps both, so a label change
on your phone never erases the data on your laptop.

## Deletions

When you delete something, the data is **removed from disk right away** and Collectary records a tiny
deletion marker so the deletion travels to your other devices. A deletion always wins: once you've
deleted an item it stays deleted everywhere, even if another device was offline and still had the old
copy. (There's no undo — see the relevant page for the feature you're deleting.)

If the shared folder is simply *missing* an item — say you pointed sync at a fresh or different folder,
or restored your database from a backup — Collectary treats that as "nothing to pull here," **not** as
a deletion, so your local collection is never wiped by an empty or unfamiliar folder.

## Where sync works

Sync runs on the **desktop** app (shared folder, OneDrive, or Google Drive) and on **Android**
(shared folder or OneDrive). The in-browser [demo](../demo.md) has no persistent filesystem, so sync
isn't meaningful there.

!!! note "Signing in to OneDrive on Android"
    The first time you choose OneDrive on Android, sign-in opens a secure browser tab and returns to
    Collectary automatically. Your sign-in is stored in the device's encrypted keystore, so you stay
    signed in between launches.
