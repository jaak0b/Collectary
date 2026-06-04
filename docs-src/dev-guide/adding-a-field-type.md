# Adding a Field Type

The field system is the most extensible part of Collectary. Adding a new `FieldDefinition` subtype
is deliberately a **localized** change: a new field type requires **zero changes outside its own
files** — dispatch is virtual and registration is by type name, so there are no type-switches to
update anywhere in the codebase.

## The pattern

To add a new field type (say `RatingFieldDefinition`):

1. **Domain** — add the `FieldDefinition` subclass in
   `src/Collectary.Core/Domain/Fields/`, plus its corresponding field-value type. Put all
   type-specific behaviour here via virtual dispatch.
2. **Editor ViewModel** — add a field-editor view model in `src/Collectary.Presentation/ViewModels/`.
3. **View** — add the `.axaml` editor view (and, if needed, a list-cell view) in
   `src/Collectary.UI/Views/`. Remember `x:DataType` on every `DataTemplate` (compiled bindings).
4. **Register once** — add a single keyed registration in `UiModule`
   (`src/Collectary.UI/DI/UiModule.cs`), keyed by the field type's name. The
   `FieldEditorRegistry` and `ListCellBuilder` resolve by `definition.GetType().Name`, so this one
   registration is all the wiring needed.

That's it — no edits to existing field types, registries, or any central switch.

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
