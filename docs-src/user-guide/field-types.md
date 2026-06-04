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

## Dates and time

**Date** is a plain calendar date — a release date, the day you bought something. **Time** is a
clock time on its own, and **Duration** is a length of time rather than a point in it, which is
what you want for a film's runtime or a board game's playing time.

## Picking from a list

Sometimes the answer should come from a fixed set of options you've decided on in advance. **Single
Choice** gives you a dropdown and lets you pick exactly one. **Multi Choice** is the same idea but
with checkboxes, so you can tick as many as apply. And for the simplest case of all — a plain
yes-or-no — **Bool** is a single toggle.

## Contact details and links

Three types cover the obvious cases here: **URL** for a web link, **Email** for an email address,
and **Phone** for a phone number.

## Pictures and colour

**Image** attaches a picture to the item, which is lovely for cover art or a photo of the real
thing. **Color** stores an actual colour value and lets you express it however you like — ARGB,
RGB, Hex, or CMYK.

## Lists within an item

Now and then a single item needs a little list of its own. Think of an album that has a list of
tracks, where each track has its own title and length. That's what **List** is for: a repeating
sub-list inside one item, where every entry carries its own set of fields. Its companion, **List
Entry**, is how you describe what those per-entry fields should be.

## The one special field

Every collection has exactly one **Display Name** field. It doesn't hold new information so much as
point at the field whose value should be used to label each item in your lists — the title of the
book, the name of the plant. You'll set this when you design the collection and rarely think about
it again.

!!! tip "Missing a type you need?"
    Collectary's field system is built to grow. If you're comfortable in the code, adding a brand-new
    field type only touches that type's own files — there's a walkthrough in
    [Adding a field type](../dev-guide/adding-a-field-type.md).
