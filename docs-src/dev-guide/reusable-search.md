# Reusing the search library

Collectary's search is split into three standalone packages so you can drop the same
JQL-style query experience into a completely different project — even one that has nothing to do
with Collectary and isn't an Avalonia app at all.

- **`Collectary.Search`** — the engine. Pure .NET with **no third-party dependencies**. It does the
  whole pipeline: turn query text into a syntax tree, bind that tree against your fields, push a
  filter down to your database, evaluate the rest in memory, and sort the results. It is generic
  over your own item type, so it never needs to know what an "Item" is.
- **`Collectary.Search.ViewModels`** — the presentation logic: the `ItemQueryViewModel`,
  `BasicFilterViewModel`, and `SearchBarViewModel` plus the `ILocalizationProvider` seam. It depends
  on the engine and CommunityToolkit.Mvvm — **no Avalonia** — so the same view-models drive any
  XAML UI framework (or none, in a test harness).
- **`Collectary.Search.Avalonia`** — the optional Avalonia layer: just the chip-bar / advanced-box
  `SearchBar` control. It depends on Avalonia and the view-models, plus the engine itself (its XAML
  surfaces a few engine types, such as query suggestions).

If you only want the query engine, take the first package and ignore the rest; if you want the
view-models without Avalonia, take the first two.

## Using the engine

The engine talks to your world through small interfaces. You implement them for your own item and
field types; everything else comes for free.

### 1. Describe how to read your items

Most apps store an item as a bag of typed values, each tagged with the id of the field it belongs
to. If that sounds like your model, hand the engine an `ItemValueModel<TItem, TValueBase>` that
tells it how to read those values — both at runtime and as an expression tree your database can
translate:

```csharp
var model = new ItemValueModel<Widget, WidgetValue>(
    values: w => w.Values,
    definitionId: v => v.FieldId,
    isEmpty: v => v.IsEmpty,
    valuesExpression: w => w.Values,
    definitionIdExpression: v => v.FieldId);
```

If your model is nothing like a value bag, skip this and implement `IFieldConditionMatcher<TItem>`
directly — it has just two members: an optional server-side `Expression<Func<TItem, bool>>` and an
in-memory `Matches(item, …)`.

### 2. Turn fields into matchers

The engine ships ready-made helpers for the common cases, all built on the value model above:

- `ComparableFieldSearch<…>` for numbers, dates, durations — anything ordered (`=`, `!=`, `<`, `>=`,
  `in`, `is empty`, …).
- `StringFieldSearch<…>` for text (`~` contains, `=`, `in`, …), case-insensitive.
- `StringListFieldSearch<…>` for tag-like multi-value fields.

Each one knows how to produce both the in-memory predicate and the database expression, so a search
runs as a SQL superset first and an exact in-memory pass second.

### 3. Expose a catalog and run a query

Implement `ISearchCatalog<TItem>` to answer two questions — "is this a known field label?" and
"which fields back this label?" — then wire the pieces together:

```csharp
var parsed   = new QueryParser(new QueryLexer()).Parse(text);
var bound    = new QueryBinder<Widget>(catalog).Bind(parsed.Query!);
var filter   = new ServerFilterBuilder<Widget>().Build(bound.Query!.Root);   // -> your DB
var matched  = candidates.Where(w => new QueryEvaluator<Widget>().Matches(bound.Query.Root, w));
var ordered  = new QueryEvaluator<Widget>().Sort(matched, bound.Query.OrderBy);
```

That's the entire engine. It never references Collectary — the engine's own test suite proves it by
driving this pipeline end to end over a throwaway fake item type.

## Adding the Avalonia UI

The `Collectary.Search.Avalonia` package adds the `SearchBar` control, which binds to the three
view-models from `Collectary.Search.ViewModels`: `ItemQueryViewModel` (advanced box + autocomplete),
`BasicFilterViewModel` (the Jira-style chip bar), and `SearchBarViewModel` (which owns the two and
the basic↔advanced switch). The view-models depend only on the engine, so you keep your own data
layer.

Two seams let you fit them to your app without dragging Collectary along:

- **Localization** — the view-models never reach for a static string table. You pass an
  `ILocalizationProvider` (a single `string Get(string key)` method); supply your own resx, JSON,
  or hard-coded strings.
- **Running a search** — `ItemQueryViewModel` takes an `ISearchRunner` that returns a
  `SearchOutcome` (items plus any errors and notices). Implement it over the engine call above, or
  over an existing search service.

### The localization key contract

The keys the package will ask your provider for are not a guessing game: they all live as
constants on `SearchLocalizationKeys`, and `new SearchLocalizationKeys().All` hands you the
complete list at runtime — handy for a startup check that your string table covers everything.
A key your provider doesn't know should fall back to returning the key itself; that keeps the UI
functional (if ugly) instead of crashing. The full contract:

| Key | Used for |
|---|---|
| `Search` | the search button label |
| `SearchPlaceholder` | watermark in the advanced query box |
| `SearchItemsPlaceholder` | watermark in a free-text chip |
| `SearchSwitchToBasic` / `SearchSwitchToAdvanced` | the mode-switch links |
| `SearchTooComplexForBasic` | message when a query can't round-trip into chips |
| `SearchMore` | the "more fields" chip button |
| `SearchFindFields` / `SearchFindValues` | watermarks in the field and value pickers |
| `SearchAllValues` | chip summary when nothing is selected |
| `SearchSelectedCount` | chip summary for multiple selections (one `{0}` placeholder) |
| `SearchContainsLabel` / `SearchEqualsLabel` | operator hint inside a free-text chip |
| `SearchValuePlaceholder` | watermark in a chip's free-text box |
| `SearchClear` / `SearchRemoveFilter` | chip flyout actions |
| `SearchSortBy`, `SearchSortNone`, `SearchSortAscending`, `SearchSortDescending` | the sort controls |
| `SearchFailed` | message when the search runner throws |
| `SearchSyntaxError`, `SearchUnknownField`, `SearchFieldNotSearchable`, `SearchOperatorNotSupported`, `SearchInvalidValue` | per-error-code query messages (each takes one `{0}` placeholder) |
| `SearchNoticeSkipped` | notice shown when a condition was skipped (one `{0}` placeholder) |

The `SearchBar` is responsive: on a wide window the items search, chips, sort, and mode controls sit
on one row. It measures what that row actually needs and, once it no longer fits — a phone, a docked
panel, a split view — it collapses **only the chips** behind a **Filters (n)** toggle. Sorting stays
on the top row as one compact `Sort by: …` dropdown button (a popup with the field picker and the
ascending/descending switch), and the advanced-mode button sits next to the Filters toggle. Expanding
the toggle drops the chips onto a second row; the sort button never gets a row of its own. That all
happens automatically from the control's own width, so you don't have to do anything to get a usable
layout on Android or a narrow desktop.

Collectary itself is just one more consumer: it adapts its `ISearchFieldCatalog`/`IItemSearchService`
and its `LocalizationService` to these interfaces and hosts the `SearchBar` control inside the
preset detail view. Your app does the same with your own types.
