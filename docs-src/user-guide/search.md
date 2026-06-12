# Search

Every collection screen has a search bar right above the item list, and it comes in two flavours.
**Basic mode** is a row of filter chips you click together — no syntax to learn. **Advanced mode**
is a query language for everything the chips can't say. The two are views of the same search: you
can switch back and forth at any time, and Collectary remembers which one you used last.

## Basic mode

When you open a collection in basic mode you'll see a small text box, a chip reading
*collection: Your Collection*, a **+ More** button, and a sort picker — much like Jira's basic
search, if you know it.

- **The text box** filters by item name as you type — `loco` finds every item whose name contains
  it.
- **Chips** are little dropdown buttons, one per field you're filtering on. Click one and a small
  panel opens: for choice-like fields (single/multi choice, yes/no, collections) you get a
  searchable checkbox list — tick one value for "equals", tick several for "any of these". For
  everything else you get a single input that filters by *contains* for text-like fields and
  *equals* for numbers and dates.
- **+ More** opens a searchable list of every field you can filter on — across all your
  collections, plus the built-ins like `created` and `updated`. Picking one adds a chip; a chip
  with nothing ticked shows *All* and simply doesn't filter.
- **Sort by** picks a field and direction for the result order.

There is no Search button in basic mode — every change runs the search right away. All chips
combine with AND: an item must match the text box *and* every chip that has a selection.

## Advanced mode

Click **Advanced** and the bar turns into a text box pre-filled with exactly what your chips were
saying — `collection = Trains AND Status in (open, done) ORDER BY Name`, say. That makes basic mode
a nice way to *learn* the query language: build a filter with chips, switch over, and read what it
wrote.

Going the other way, **Basic** converts your text back into chips whenever it's a plain AND of
simple conditions. Queries using `OR`, `NOT`, parentheses, comparisons like `>`, `is empty`, or
several sort fields are more than the chips can express — Collectary tells you so and stays in
advanced mode, with your query untouched. One small wrinkle: your text is re-written in the bar's
canonical form when you convert, so spacing and quoting may change while the meaning stays the
same.

## Writing a query

A query is a list of conditions joined with `AND` and `OR`, with optional parentheses and an
optional `ORDER BY` at the end. A few examples:

```text
Status = open
Status = open AND Price > 50
preset = "Books" AND Price > 3 OR (Author != "Twain" AND Pages in (100, 200))
Tags ~ rare ORDER BY Price DESC, name
```

As you type, suggestions pop up under the box: first your field names, then the comparisons that
field understands, then — for choice fields — the values it can take. Use ↑/↓ to pick one, Enter or
Tab to accept it, Escape to dismiss the popup. Press Enter (or the Search button) to run the query.

## Fields

Conditions name fields by their **label**. The match is across collections: if both *Books* and
*Games* have a field labelled `Price`, then `Price > 50` matches items in both — even when one is a
currency field and the other a plain number.

A few built-in names always work, no matter what your fields are called:

| Name | Meaning |
|---|---|
| `name` | the item's display name |
| `preset` (or `collection`) | the collection the item belongs to, by name |
| `created` / `updated` | when the item was created / last changed |

If a field's label collides with one of these, both meanings apply — the condition matches when
either does. Field names with spaces go in quotes: `"Purchase Price" > 10`.

Fields that hold images, audio, file attachments, or nested lists can't be searched (yet) — they
don't show up in the suggestions, and typing one by hand gives you a clear message instead of an
empty result.

## Comparisons

| Operator | Meaning | Works on |
|---|---|---|
| `=` / `!=` | equals / does not equal | everything |
| `<` `<=` `>` `>=` | less / greater | numbers, dates, ratings, durations, weights… |
| `~` / `!~` | contains / does not contain | text-like fields, tags, multi-choice |
| `in (a, b, c)` / `not in (…)` | matches any of the listed values | everything |
| `is empty` / `is not empty` | the field has no value / has one | everything |

Text matching ignores upper/lower case for A–Z. Values with spaces go in quotes
(`Status = "On Hold"`), numbers use a dot as the decimal separator (`Price > 10.5`), and dates are
easiest written as `2025-06-15`. Currency values may include the symbol (`Price > €10`), percentages
the `%` sign, and durations understand `90`, `1:30`, and `1h 30m`.

Dates also work in the format your app language uses — a German user can write
`created = 13.06.2026` just as well as `created = 2026-06-13`. A date always means *your* calendar
day: `created = 2026-06-13` finds everything you created on June 13th your time, even if the clock
in another timezone had already rolled over to the next day.

Measurements and weights understand units. A bare number compares amounts regardless of unit
(`weight = 500` finds both *500 g* and *500 kg*), while a value with a unit only matches that unit:
`weight = "500 g"` finds the 500-gram item and leaves the 500-kilogram one alone. Units are matched
by name, not converted — `weight > "1 kg"` won't find an item stored as *1500 g*. Sorting by such a
field orders by the raw number, ignoring units.

`!=`, `!~`, and `not in` only match items that *have* a value for that field — an item without a
`Status` at all is not matched by `Status != open`. Use `is empty` to find those.

## Sorting

Finish the query with `ORDER BY` and one or more fields, each optionally followed by `DESC`:

```text
preset = "Books" ORDER BY Author, Published DESC
```

Sorting works across collections too; items without a value for the sort field go last.

## Searching across collections

Delete the pre-filled `preset = …` clause (or just write your own query without one) and the search
runs over every collection you can see. When the results span more than one collection, an extra
**Collection** column appears so you can tell where each item lives, and opening an item takes you
to its own editor with its own fields.

## Next steps

- [Collections](collections.md)
- [Field types reference](field-types.md)
