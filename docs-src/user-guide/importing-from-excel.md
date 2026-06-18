# Importing from Excel

Already have a collection in a spreadsheet? You don't need to retype it. Collectary can read an
Excel workbook (`.xlsx`) and turn its rows into items — either adding them to a collection you
already have, or building a brand-new collection from the sheet.

Collectary can also import **CSV files** — see [CSV files](#csv-files) at the end. Both importers share
the exact same steps; only the file you pick differs.

## Starting an import

1. On the home screen, open the **＋ New Collection** menu and choose **Import from Excel**.
2. Pick your `.xlsx` file.

The import opens as a short, guided flow with a few steps. You can go **Back** at any point, or
**Cancel** to bail out — nothing is saved until the final step.

## Step 1 — Pick a worksheet

A workbook can hold several sheets. Choose the one you want to import. (Importing several sheets at
once is a future addition — for now, run the import again for each sheet.)

## Step 2 — Check the data

You'll see a live preview of the sheet in a table. Two switches control how Collectary reads it:

- **First row is a header** — when on, the top row supplies the column names instead of becoming an
  item. Turn it off if your sheet jumps straight into data.
- **Rotate 90°** — flip the sheet when your fields run *down* the page instead of *across* it (each
  field is a row, each item a column). The preview updates instantly so you can see which way is right.

There's also a **Source number & date format** picker. Collectary makes a sensible guess, but if your
sheet was made on a computer with a different language — say a German sheet (`1.234,56`, `31.12.2024`)
opened on an English machine — set the format here so numbers and dates are read correctly. Cells that
Excel already stores as real numbers or dates are understood automatically, whatever your settings.

## Step 3 — Choose where the data goes

- **Add to an existing collection** — pick one of your collections. Each spreadsheet column will be
  matched to one of that collection's fields.
- **Create a new collection** — give it a name. Collectary inspects each column and suggests a field
  type (text, number, date, and so on), which you can change.

## Step 4 — Map the columns

This is where you tell Collectary what each column means. The columns are laid out as a table — one row
per spreadsheet column — with a header strip across the top so you can read straight down. Each row has,
left to right:

- an **Include** tick — clear it to leave that column out of the import;
- the **Column** itself, showing its heading and a few sample values so you can tell what's in it;
- a **Name** radio — exactly one column becomes each item's name, and choosing a different row moves the
  mark automatically, so you can never pick two by mistake. The row you mark as the Name doesn't also map
  to a field; its mapping greys out to show it's spoken for.
- the **mapping** for everything else. This is the only part that differs by the choice you made in
  Step 3:
  - **Into an existing collection** — pick the field each column fills from a drop-down. Field types
    that can't come from a spreadsheet — images, file attachments, audio, linked items, nested lists —
    are greyed out in the list, so you always see they exist but can't pick them.
  - **Into a new collection** — give each column a **field name** (pre-filled from its heading) and a
    **type** (Collectary suggests one from the data, which you can change).

On a **phone** (or any narrow window) the table would be too wide, so the same step shows one **card per
column** stacked down the screen instead: the heading and samples on top, a "use this column as each
item's name" toggle, then the field drop-down (or field name and type) below. The choices are exactly the
same — only the arrangement changes to fit the screen.

## Step 5 — Done

Collectary imports the rows and shows a summary:

- how many items were imported,
- any rows it had to skip (for example, a row missing a required field), with the reason,
- any individual cells it couldn't read (for example, the word "soon" in a number column) — those are
  left blank and the rest of the row still comes in.

Click **Done** to jump straight to the collection and see your imported items.

## CSV files

A CSV (comma-separated values) file is plain text — one row per line, fields separated by a delimiter.
It's what you get from "Save As → CSV" in Excel, Numbers, Google Sheets, and countless other tools.

To import one, open the **＋ New Collection** menu and choose **Import from CSV**, then pick your `.csv`
file. From there the steps are identical to the Excel import above: preview, choose a target, map the
columns, done.

A few CSV-specific notes:

- **Delimiter is detected automatically.** Comma, semicolon, and tab files all work. This matters for
  files exported on a German-language computer, where Excel uses a semicolon (`;`) as the separator and
  a comma inside numbers (`1.234,56`).
- **Quoted fields are understood**, including values that contain the delimiter or span multiple lines
  (e.g. `"Dune, the novel"`), and doubled quotes (`""`) inside a quoted value.
- A CSV has **no built-in number or date formatting**, so the **Source number & date format** picker on
  the preview step is your friend — set it to match the machine the file came from. Values that are
  already written in the universal form — a plain number like `1234.56` or an ISO date like `2024-12-31` —
  are recognised on their own and read the same way no matter what that picker is set to, so a file
  exported by a program rather than a person just works.

## Auto-number columns

If your spreadsheet already has its own running number — an article number, a catalogue ID, that kind
of thing — you can bring it in as an **Auto-number** field. Map the column (or pick the Auto-number type
when building a new collection) and Collectary keeps the numbers exactly as they are in the sheet; it
never renumbers or overwrites them. Imported auto-number fields come in **editable**, so you can adjust a
value later. If the same number turns up twice — repeated within the sheet, or clashing with a number
already in the collection — the import doesn't block: it brings the rows in and **lists the duplicates on
the final summary** so you can tidy them up. New items you add after the import still get their next number
assigned automatically, and a cell left blank in the sheet simply comes in without a number (it gets one
the first time you open that item).

## When a value doesn't fit

The import never invents data. If a cell can't be read as the field you mapped it to — a word in a
number column, a value that isn't one of a choice field's options, a date range that ends before it
starts — that cell is left out and the row is listed on the final **Check the data** summary, so you
always know exactly what was skipped and why. Mapping two spreadsheet columns onto the same field keeps
the first and ignores the rest, so a collection never ends up with two conflicting values for one field.

## What can't be imported

Anything that isn't plain text in a cell: images, file attachments, audio, links to other items, and
nested lists. These fields stay in your collection — they're just not filled in by the import, and you
can add them by hand afterwards.
