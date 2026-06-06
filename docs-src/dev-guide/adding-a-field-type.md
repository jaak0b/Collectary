# Adding a Field Type

Adding a new `FieldDefinition` subtype is a **localized** change: it requires **zero changes outside
its own files**. Dispatch is virtual and registration is by type name, so there are no type-switches
to update anywhere.

## The pattern

To add a new field type (say `RatingFieldDefinition`):

1. **Domain** — add the `FieldDefinition` subclass in
   `src/Collectary.Core/Domain/Fields/`, plus its corresponding field-value type. Put all
   type-specific behaviour here via virtual dispatch. Decorate the class with three attributes:
   - `[LocalizedName("FieldType_Rating")]` — the resx key for the type's display name.
   - `[FieldIcon(IconGlyphs.Star)]` — the icon shown beside it in the menu. Reference a named
     constant from `IconGlyphs` (see "Icons" below) rather than a literal — never an emoji.
   - `[FieldCatalog(order, FieldCategory.Visual)]` — marks it as **user-addable** and places it in
     the "Add field" menu. Omit this attribute only for types that must never be added by hand
     (the way `DisplayNameFieldDefinition` does).
2. **Editor ViewModel** — add a field-editor view model in `src/Collectary.Presentation/ViewModels/`.
3. **View** — add the `.axaml` editor view (and, if needed, a list-cell view) in
   `src/Collectary.UI/Views/`. Remember `x:DataType` on every `DataTemplate` (compiled bindings).
4. **Register once** — add a single keyed registration in `UiModule`
   (`src/Collectary.UI/DI/UiModule.cs`), keyed by the field type's name. The
   `FieldEditorRegistry` and `ListCellBuilder` resolve by `definition.GetType().Name`, so this one
   registration is all the wiring needed.

That's it — no edits to existing field types, registries, menus, or any central switch.

## The "Add field" menu is data-driven

You never touch a menu when adding a type. `FieldTypeCatalog`
(`src/Collectary.Presentation/ViewModels/FieldTypeCatalog.cs`) discovers every `FieldDefinition`
carrying a `[FieldCatalog]` attribute by reflection and orders them by `(category, order)`. Both the
**preset editor** (Collection Settings) and the **Shared Fields library** render this one catalog, so
their menus are always identical — a type added with `[FieldCatalog]` appears in both automatically.
A guard test (`FieldCatalogAttributeTest`) fails the build if a new addable type forgets the attribute.

## Icons

Every icon in the app — field types, collection templates, and bits of chrome like the sidebar
toggle — comes from a single embedded icon font, `CollectaryIcons.ttf`
(`src/Collectary.UI/Assets/Fonts/`). It's a slimmed-down, renamed subset of Microsoft's MIT-licensed
[FluentUI System Icons](https://github.com/microsoft/fluentui-system-icons), carrying only the glyphs
we actually use. Emoji are avoided because the browser (WASM) build has no system fonts to fall back
on, so emoji render as empty boxes there; embedded vector glyphs look identical on every platform and
recolour with the theme.

Each glyph has a friendly name in `IconGlyphs` (`src/Collectary.Core/Domain/Fields/IconGlyphs.cs`),
e.g. `IconGlyphs.Star`. In C# you reference the constant (`[FieldIcon(IconGlyphs.Star)]`); in XAML you
bind it and tag the `TextBlock` with `Classes="icon"` so it picks up the icon font:

```xml
<TextBlock Text="{x:Static icons:IconGlyphs.Folder}" Classes="icon"/>
```

**To add a new icon:** pick one from the Fluent set, find its `_20_regular` codepoint in the font's
metadata, add it to the subset list and re-run the subsetting step, then add a named constant to
`IconGlyphs`. The `IconFontTest` guard fails if any `IconGlyphs` constant has no matching glyph in the
embedded font, so a typo or a missing codepoint can't slip through.

## Required tests

Per the project's testing rules, a new field type needs at minimum:

- `<Type>FieldDefinitionTest`
- `<Type>FieldValueTest`
- an entry in `FieldEditorMapperTest`
- `<Type>FieldEditorViewModelTest`

These must be written **test-first** (red before green) and the change must hold the coverage and
mutation gates. See [Testing](testing.md).

## Rules of thumb

- **Never** introduce a `switch` on field type. If you feel the urge, the behaviour belongs on the
  `FieldDefinition` subclass as a virtual member.
- If a field type would be complex, ship a **simple version first** plus an on-screen note rather
  than silently skipping the use case.
- Adding the type touches its own files plus one line in `UiModule` — nothing else.
