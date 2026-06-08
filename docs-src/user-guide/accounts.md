# Profiles

Collectary uses **profiles**, like a streaming app: a row of tiles, one per person. There are no
passwords and no email — a profile is just a name. Each profile keeps its own collections, which can
still be [shared](sharing.md) between profiles.

## Picking a profile

The app opens on the profile screen — *"Who's collecting?"* — with a tile per profile, each showing
a coloured avatar and the name. Tap a tile to enter as that profile. The picker is part of the main
window rather than a separate pop-up, so it works the same on desktop and phone.

## Adding a profile

Tap **Add profile**, type a name, tap **Create**. You're taken straight into the app as that
profile. If the name is already in use, Collectary keeps it for display but makes the underlying
identifier unique so sharing still has something unambiguous to point at.

## Switching profiles

Inside the app, the **Switch profile** button is at the top-right, next to the sync controls. Tap it
to return to the profile screen without closing the app. There's also a **Switch profile** button
under **Account** in [Settings](settings.md).

## Remembering where you left off

Collectary opens straight into the last profile you used, skipping the picker. Use **Switch profile**
to choose a different one. If the remembered profile was removed, you'll see the picker again.

## Deleting a profile

Under **Account** in [Settings](settings.md), **Delete this profile** removes the profile you're
currently signed in as. Because a profile owns its collections, deleting it **also deletes every
collection that profile owns and everything in them** — the confirmation tells you how many will go, so
read it before confirming. There's no undo. Afterwards you land back on the profile screen.

If you only want to step away, use **Switch profile** instead — that leaves the profile and its
collections untouched.

When [sync](sync.md) is set up, a deletion travels to every device sharing the folder, so the profile
and its collections disappear everywhere on the next sync — just like a collection you delete or a
share you revoke. Collections **shared with** the deleted profile (owned by someone else) are not
touched.

## Profiles and collections

- Every collection has an **owner** — the profile that created it.
- The owner can [share](sharing.md) a collection with other profiles, granting **Read** or **Write**
  access.
- Edits are attributed to the profile that made them, which feeds [sync](sync.md) conflict handling.
