# Field Types

When you add a field to a collection, you pick a **type** for it. The type is what decides how you
type the value in (a calendar popup for a date, a star control for a rating, a colour picker for a
colour) and how that value looks when it's shown back to you in a list. Choosing the right type
mostly means thinking about the kind of information you're storing — here's a tour of what's on
offer so you can match each to the job.

## Text and writing

For plain words there's **Text**, which is a single line — perfect for a title, a name, or a short
note. When you need room to write properly, with bold and lists and the like, reach for **Rich
Text** instead. And if you'd rather attach a loose handful of keywords than write a sentence,
**Tags** lets you do exactly that, showing each keyword as its own little chip.

## Numbers

If your value is a whole number — a quantity, a year, a count of how many you own — use **Integer**.
When fractions matter, like a weight or a measurement, **Decimal** is the one. **Percentage** is a
number from 0 to 100 that shows up with a `%` sign, and **Currency** is money: an amount paired with
a currency symbol, ideal for what you paid or what something's worth. For "how much do I like this?"
there's **Rating**, the familiar row of stars.

## Sizes and weights

**Measurement** records a size with its unit — a coin's diameter in millimetres, a watch case, the
scale of a model — so the number always carries its meaning. **Weight** does the same for how heavy
something is, in grams, ounces, kilograms, or pounds, which is handy for bullion coins or anything
you'd ship.

## How complete is it?

Collectors love a set that's nearly finished. **Progress** records "owned X of Y" — 42 of 151 cards,
say — and draws a little bar so you can see at a glance how close you are. It's distinct from a plain
percentage because it remembers both numbers.

## A slider instead of typing

Sometimes a number feels better as a dial than a text box. **Slider** gives you a 0–100 track you
drag to set a value — condition, intensity, how full a set is — with the exact number shown beside
it.

## Dates and time

**Date** is a plain calendar date — a release date, the day you bought something. **Time** is a
clock time on its own, and **Duration** is a length of time rather than a point in it, which is
what you want for a film's runtime or a board game's playing time.

## A span of time

When one date isn't enough, **Date Range** captures a from–to span — how long you owned something,
a wine's drinking window, the era a model was made. You get two date pickers and the range reads back
neatly as "start – end".

## Picking from a list

Sometimes the answer should come from a fixed set of options you've decided on in advance. **Single
Choice** gives you a dropdown and lets you pick exactly one. **Multi Choice** is the same idea but
with checkboxes, so you can tick as many as apply. And for the simplest case of all — a plain
yes-or-no — **Bool** is a single toggle.

## Contact details and links

Three types cover the obvious cases here: **URL** for a web link, **Email** for an email address,
and **Phone** for a phone number.

## Where it's from

For coins, stamps, wine, or whisky, the country of origin matters — so **Country** lets you pick one
from a list instead of typing it, showing the flag alongside the name. Because everyone records the
same value, you can later group or filter your collection by country and have it actually line up.

## Scanning a barcode or QR code

Typing a long product number by hand is no fun, so **Barcode / QR** lets you scan it instead. Press
**Scan…** and point your webcam (or phone camera) at the code, or just hand it a photo you've already
taken — Collectary reads the image and fills in the code for you. It understands the usual suspects:
EAN-13 and UPC for retail products, ISBN for books, plus QR, Data Matrix, PDF417 and more, so it's
equally at home cataloguing a shelf of games or a box of LEGO sets. No camera handy? You can always
just type the code in; the field never blocks you.

## Attaching documents

Some things come with paperwork. **File Attachment** lets you keep those documents right next to the
item — a manual, a warranty, a certificate of authenticity, a receipt, or the building instructions
for a brick set. Add as many as you like; each one opens straight back out when you need it.

## Making your own QR labels

The flip side of scanning is **QR Code**, which turns a bit of text into a QR you can print and
stick on things. Type in a shelf code, a box number, or a link back to the item, and Collectary
draws the matching QR right there in the editor. Pair it with the scanner field and you've got a
tidy little loop: label a box now, scan it later to pull the item straight up.

## Pictures and colour

**Image** attaches a picture to the item, which is lovely for cover art or a photo of the real
thing. When one picture isn't enough — the front and back of a coin, a few angles of a figure, or
condition shots of a card — reach for **Image Gallery**, which keeps several pictures together in
the order you choose. **Color** stores an actual colour value and lets you express it however you like — ARGB,
RGB, Hex, or CMYK.

## Lists within an item

Now and then a single item needs a little list of its own. Think of an album that has a list of
tracks, where each track has its own title and length. That's what **List** is for: a repeating
sub-list inside one item, where every entry carries its own set of fields. Its companion, **List
Entry**, is how you describe what those per-entry fields should be.

## A spoken note

Sometimes it's quicker to say it than type it. **Audio Note** records a short clip — your spoken
impressions of a wine or whisky, the condition of a piece, the way a name is pronounced — and keeps
it with the item to play back later.

## Linking items together

Collections aren't always flat. **Linked Item** points one item at another — a minifig at the set it
came in, a lens at the camera body it pairs with, a card at the deck it lives in. Open the dropdown
and Collectary lists your other items to choose from, then remembers the link by name.

## The one special field

Every collection has exactly one **Display Name** field. It doesn't hold new information so much as
point at the field whose value should be used to label each item in your lists — the title of the
book, the name of the plant. You'll set this when you design the collection and rarely think about
it again.

!!! tip "Missing a type you need?"
    Collectary's field system is built to grow. If you're comfortable in the code, adding a brand-new
    field type only touches that type's own files — there's a walkthrough in
    [Adding a field type](../dev-guide/adding-a-field-type.md).
