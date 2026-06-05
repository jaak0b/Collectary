# Backup & restore

Sometimes you don't want an ongoing [sync](sync.md) — you just want a single file you can tuck away
on a USB stick, drop into cloud storage, or hand to another device. That's what **backup & restore**
is for: it packs your *entire* collection into one portable file and reads it back later.

## What's in a backup

A backup is a single `.collectary` file (a zip archive under the hood) that bundles **everything**:

- every collection, item and system field, and
- every attached **image** *and* **document** — gallery photos, manuals, certificates, audio notes,
  the lot.

Because it's all in one file, there are no loose folders of pictures to keep together. Move the one
file and you've moved the whole collection.

## Making a backup

1. Open [Settings](settings.md) and find **Backup & restore**.
2. Click **Export to file…**.
3. Pick where to save it. Collectary suggests `collection-backup.collectary`, but you can name it
   anything.

That's it — the file now holds a complete snapshot.

## Restoring a backup

1. In **Settings → Backup & restore**, click **Import from file…**.
2. Choose a `.collectary` file.

Importing **merges** the backup into what you already have, comparing each entry by its revision:

- Anything new (or newer) in the backup is brought in.
- Anything you've changed locally since is left alone — your local edits are never silently
  overwritten, and nothing you have that the backup *doesn't* is ever deleted.
- If the same entry was changed on both sides, it's flagged as a **conflict** and your local version
  is kept. Collectary tells you which entries clashed so you can review them.

After an import you'll see a short summary — how many entries were brought in, and any conflicts that
kept your local copy.

!!! tip "Backup vs. sync"
    [Sync](sync.md) is for keeping devices continuously in step through a shared location. A backup
    is a one-shot, self-contained file — perfect for archiving, moving to a new machine, or keeping a
    safety copy. They're independent: using one doesn't affect the other.

!!! note "Desktop feature"
    Export and import use your system file picker, so they run in the desktop app. The in-browser
    [demo](../demo.md) has no persistent filesystem.
