# Profiles

Collectary uses **profiles**, the way a streaming app does: a row of tiles, one per person, and you
tap a tile to start. There are no passwords and no email — a profile is just a name. Each profile
keeps its own collections, and collections can still be [shared](sharing.md) between profiles.

## Picking a profile

When you open the app you land on the profile screen — *"Who's collecting?"* — with a tile for each
profile. Each tile shows a coloured avatar with the profile's initial and the name underneath. Tap a
tile to enter the app as that profile.

The picker is part of the app itself: it shows up right inside the main window rather than as a
separate pop-up, so it works the same on desktop and on a phone, and the tiles reflow to fit a narrow
screen. Once you're in, you stay on that same single screen the whole time.

## Adding a profile

On the profile screen, tap **Add profile**, type a name, and tap **Create**. That's the whole form —
just a name. The new profile is created and you're taken straight into the app as that profile.

If you pick a name that's already in use, Collectary keeps the name you typed for display and quietly
makes the behind-the-scenes identifier unique, so sharing still has something unambiguous to point at.

## Switching profiles

Once you're inside the app, the **Switch profile** button sits at the top-right, next to the sync
controls, showing the current profile's name. Tap it to drop back to the profile screen — without
closing the app — so you (or someone else) can jump into a different profile straight away. There's
also a **Switch profile** button under **Account** in [Settings](settings.md).

## Remembering where you left off

Collectary remembers the last profile you used and takes you straight into it next time you open the
app, skipping the picker. Use **Switch profile** whenever you want to choose a different one. If the
remembered profile has since been removed, you'll see the picker again.

## How profiles relate to collections

- Every collection has an **owner** (the profile that created it).
- The owner can [share](sharing.md) a collection with other profiles, granting **Read** or **Write**
  access.
- Edits are attributed to the profile that made them, which also feeds into [sync](sync.md) conflict
  handling.
