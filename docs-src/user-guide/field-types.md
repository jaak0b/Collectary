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
- **Decimal** — a number with fractions.
- **Percentage** — 0–100, shown with a `%`.
- **Currency** — an amount plus a currency symbol.
- **Rating** — a row of stars.
- **Slider** — a 0–100 track you drag, with the value shown beside it.
- **Progress** — "owned X of Y", with a bar. Keeps both numbers, unlike a plain percentage.

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
- **Bool** — a yes/no toggle.

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
- **QR Code** — turns text (a shelf code, a box number, a link) into a QR you can print.

## Files and media

- **File Attachment** — keep documents with the item: manuals, warranties, certificates, receipts,
  instructions. Add as many as you like.
- **Image** — one picture, for cover art or a photo of the item.
- **Image Gallery** — several pictures in an order you choose (front and back of a coin, a few
  angles).
- **Color** — a colour value in ARGB, RGB, Hex, or CMYK.
- **Audio Note** — a short recorded clip kept with the item.

## Structure

- **List** — a repeating sub-list inside one item, where each entry has its own fields (an album's
  tracks, each with a title and length). **List Entry** describes those per-entry fields.
- **Linked Item** — points one item at another (a minifig at its set, a lens at its camera body).
- **Display Name** — every collection has exactly one. It points at the field used to label each
  item in lists. You set it when designing the collection.

!!! tip "Missing a type you need?"
    Adding a new field type only touches that type's own files — see
    [Adding a field type](../dev-guide/adding-a-field-type.md).
