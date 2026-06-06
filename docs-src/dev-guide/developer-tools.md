# Developer Tools

Spinning up realistic data by hand — a preset, then an item with every field filled in — gets old fast
when you're testing layout or a new field editor. Collectary ships two conveniences that exist **only in
Debug builds**. A Release build (what your users run) never sees either of them, so there's nothing to
hide or feature-flag away before shipping.

## The "Developer (all fields)" template

Create a collection the usual way — **New collection → From template** — and you'll find a **Developer**
category at the bottom of the picker with a single entry, **Developer (all fields)**. Pick it and you get
a preset that contains:

- **one of every field type** Collectary knows about — text, numbers, dates, choices, ratings, colours,
  images, audio, barcodes, the lot;
- a **configured list** with a few sub-fields, so you can exercise nested entry editing;
- a **field group** ("Specifications") holding a couple of fields, so grouped layout is covered too.

It's the fastest way to see how a change to the item editor, a new field editor, or a theme tweak behaves
against the full spread of field types in one screen.

The template lives in `DeveloperTemplate.cs` and the whole class is wrapped in `#if DEBUG`. Templates are
discovered by reflection (see [Adding a field type](adding-a-field-type.md) for how the catalog works), so
a class that doesn't compile in Release simply isn't there — the Developer category disappears with it.

## "Fill random" in the item editor

Open any collection, add an item, and in a Debug build you'll see a **Fill random** button next to Save and
Cancel. One click drops believable random data — courtesy of [Bogus](https://github.com/bchavez/Bogus) —
into every field on the form. Review it, tweak what you like, and Save.

The randomisation is per-field: every field editor decides how to fill itself (a `Randomize` method it
overrides), so adding a new field type automatically slots in here too — no central switch to update.

A few field types are **deliberately left empty**, because filling them would mean writing throwaway files
to disk or inventing references that don't exist:

- **Image, Multi-image, Audio, File attachment** — these point at real blobs in the image/file store, so
  there's nothing sensible to fabricate; add a file by hand if you need one.
- **Linked item** — it references another real item, which a random value can't conjure.
- **Colour** — left untouched as well; pick a colour manually.

Everything else — including each entry of a list — gets filled. The button only changes the open form;
nothing is saved until you hit Save, exactly as if you'd typed it all in yourself.
