# Backup & restore

When you don't want ongoing [sync](sync.md), backup & restore packs your whole collection into one
portable file you can keep on a USB stick, in cloud storage, or hand to another device.

## What's in a backup

A backup is a single `.collectary` file (a zip archive) that bundles everything: every collection,
item, and shared field, plus every attached image and document — gallery photos, manuals,
certificates, audio notes. It's all in one file, so there are no loose folders to keep together.

## Making a backup

1. Open [Settings](settings.md) and find **Backup & restore**.
2. Click **Export to file…**.
3. Pick where to save it. Collectary suggests `collection-backup.collectary`; rename it if you like.

## Restoring a backup

1. In **Settings → Backup & restore**, click **Import from file…**.
2. Choose a `.collectary` file.

Importing **merges** the backup into what you have, comparing each entry by its revision:

- Anything new or newer in the backup is brought in.
- Local edits made since are left alone, and nothing you have that the backup lacks is deleted.
- If the same entry changed on both sides, it's flagged as a **conflict** and your local version is
  kept.

After an import you'll see a summary of how many entries were brought in and any conflicts.

!!! tip "Backup vs. sync"
    [Sync](sync.md) keeps devices continuously in step through a shared location. A backup is a
    one-shot file for archiving, moving to a new machine, or a safety copy. The two are independent.

!!! note "Desktop feature"
    Export and import use the system file picker, so they run in the desktop app. The in-browser
    [demo](../demo.md) has no persistent filesystem.
