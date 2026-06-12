# Field Types

Each field has a type. The type decides how you enter the value (a calendar for a date, stars for a
rating, a colour picker for a colour) and how it's shown in a list. Pick the type that matches the
kind of information you're storing.

## Text

- **Text** — a single line: a title, a name, a short note.
- **Rich Text** — formatted text with bold, lists, and so on.
- **Tags** — a set of keywords, each shown as a chip.

## Numbers

- **Integer** — a whole number: a quantity, a year, a count.
- **Auto Number** — a whole number that fills itself in: every new item gets the next free number, so
  each train (or coin, or anything you number) ends up unique without you typing it.
- **Decimal** — a number with fractions, shown to as many decimal places as you choose.
- **Percentage** — 0–100, shown with a `%`.
- **Currency** — an amount plus a currency symbol.
- **Rating** — a row of stars.

## Sizes and weights

- **Measurement** — a size with its unit (a coin's diameter in mm, a watch case).
- **Weight** — a weight in grams, ounces, kilograms, or pounds.

## Dates and time

- **Date** — a calendar date.
- **Time** — a clock time.
- **Duration** — a length of time (a film's runtime, a game's playing time).
- **Date Range** — a from–to span, shown as "start – end".

## Choices

- **Single Choice** — a dropdown, pick one.
- **Multi Choice** — checkboxes, pick any number.
- **Bool** — a yes/no toggle. By default it's a plain checkbox (ticked means yes, unticked means no).
  Turn on **Allow unanswered** if you'd rather have three states — yes, no, or left blank — so an
  item you haven't decided on yet stays empty instead of silently counting as "no".

## Links and contact

- **URL** — a web link.
- **Email** — an email address.
- **Phone** — a phone number.

## Origin

- **Country** — pick a country from a list, shown with its flag. Because everyone records the same
  value, you can group or filter by country.

## Codes

- **Barcode / QR** — scan a code with a camera or from a photo, or type it. Reads EAN-13, UPC, ISBN,
  QR, Data Matrix, PDF417, and more.

  The **Scan…** button opens a little menu with two ways in:

  - **From file…** — pick a photo of the code you already have and Collectary reads it.
  - **From camera…** — opens a live camera view right inside the app. Point it at the code and the
    moment it reads one it fills the field in and steps back automatically — no shutter button to
    press. If you have more than one camera (say a front and a back one), use **Switch camera** to
    flip between them. Tap **Cancel** to back out without scanning.

  The camera option needs a working camera, so it lights up on phones and on desktops with a webcam.
  Where there's no camera — the web version, or a desktop without one — the option is still there but
  greyed out, so you always know it exists. On Android the camera permission is asked the first time you
  pick **From camera** — not at startup — so the app never prompts for the camera unless you use it; if
  you decline, it tells you and steps back. If the camera is already busy in another app, Collectary
  tells you and steps back so nothing hangs. You can type or paste a code by hand at any time.
- **QR Code** — turns text (a shelf code, a box number, a link) into a QR you can print.

## Files and media

- **File Attachment** — keep documents with the item: manuals, warranties, certificates, receipts,
  instructions. Add as many as you like.
- **Image** — one picture, for cover art or a photo of the item.
- **Image Gallery** — several pictures in an order you choose (front and back of a coin, a few
  angles).
- **Color** — a colour value in ARGB, RGB, Hex, or CMYK.
- **Audio Note** — a short clip you record straight into the item. Press **Record** (the microphone
  button) to start and again to stop, then use the **Play/Pause** button to listen back. The button's
  tooltip reminds you that the microphone and playback device are chosen once, app-wide, under
  **Settings → Audio** — both default to your system's default device. A small **gear** button on the
  control is a shortcut straight to those settings; it saves the item first so you don't lose edits.
  Clips are saved with the item and travel with it through sync and backups. Recording works on the desktop and Android apps; in the
  browser the field tells you recording isn't available there. On Android the microphone permission is
  asked the first time you press **Record** — not at startup — so the app never prompts for the mic
  unless you actually use this field; if you decline, the field says microphone access is needed and
  records nothing.

## Structure

- **List** — a repeating sub-list inside one item, where each entry has its own fields (an album's
  tracks, each with a title and length). **List Entry** describes those per-entry fields.
- **Linked Item** — points one item at another (a minifig at its set, a lens at its camera body).
- **Display Name** — every collection has exactly one. It points at the field used to label each
  item in lists. You set it when designing the collection.

## Type settings

Some types carry extra settings you adjust when you add or edit the field, under **Type settings** in
the field panel:

- **Text** — set a **maximum length** to stop entries running on past a limit you choose. Leave it
  blank for no limit.
- **Integer** — set a **minimum** and/or **maximum** so the editor won't accept values outside the
  range. Leave either blank to leave that end open.
- **Auto Number** — three choices. **Let me edit the number** decides whether the auto-filled number
  is yours to change or stays read-only. **Next number is** picks how the next one is chosen — either
  *highest used + 1*, or the *lowest unused number* so it fills the gaps left when you delete items.
  And when editing is allowed, **if an entered number is already used** lets you choose what happens on
  a clash: block saving, just warn, or allow it. A number, once given to an item, stays with it — the
  editor only ever picks a number for a brand-new item.
- **Decimal** — choose how many **decimal places** to show, both while editing and in lists.
- **Bool** — choose between a plain yes/no checkbox and the three-state **Allow unanswered** mode.
- **Rating** — pick how many **stars** the scale runs to.
- **Currency** — set the **symbol** shown beside the amount.
- **Color** — pick the format (Hex, RGB, ARGB, CMYK).
- **Image** — set the display size and how it scales.

!!! tip "Missing a type you need?"
    Adding a new field type only touches that type's own files — see
    [Adding a field type](../dev-guide/adding-a-field-type.md).
