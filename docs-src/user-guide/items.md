# Items

An **item** is a single entry in a collection — one book, one coin, one board game. Every item has
a value for (some of) the fields defined by its collection.

## Adding an item

From a collection's view, choose to add an item. The **item editor** shows one input per field,
using the editor appropriate to each [field type](field-types.md):

- a text box for text fields,
- a number box for integers/decimals,
- a date picker for dates,
- a colour picker for colour fields,
- a star control for ratings,
- an image picker for image fields,
- and so on.

Fields are grouped according to the collection's [field groups](collections.md#field-groups).

## Editing and deleting

Open an existing item to edit any of its values, or delete it from the collection. Edits are saved
to the database immediately and are attributed to the current user.

## How items appear in lists

In a collection's list view, each item is labelled by its **display name** field, and other fields
are rendered in a compact, type-aware way (currency shows its symbol, percentages show `%`, tags
show as chips, colours show a swatch, and so on).

## Tips

- The set of fields you can fill in is controlled by the **collection**, not the item — to add a
  new property to every item, add a field in the [collection editor](collections.md#defining-fields).
- Use the [List field type](field-types.md) when a single item needs a repeating sub-list (e.g.
  the tracks on an album, with their own sub-fields).
