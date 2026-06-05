# Contributing

Thanks for your interest in improving Collectary! This page covers the conventions every change is
expected to follow. They exist to keep the codebase consistent, well-tested, and portable across the
desktop, browser, and mobile heads.

## Coding conventions

- **No static methods or properties** — except Avalonia `AvaloniaProperty.Register` and framework
  metadata. Prefer instance members so everything stays injectable and testable.
- **Localization is resx-only.** All translatable strings live in `Strings.en/de.resx` (or a
  domain-specific resx pair) and are referenced through `LocalizationService`. Both languages must
  carry every key. See [Localization](localization.md).
- **No empty catch blocks.** Log through the app logger and surface user-facing failures through the
  dialog service.
- **A new `FieldDefinition` subtype changes nothing outside its own files** — dispatch is virtual and
  registration is by type name, so there are no type-switches to update. See
  [Adding a field type](adding-a-field-type.md).
- **Missing a field type? Ship a simple version plus an on-screen note** rather than silently
  skipping the use case.
- **No trademarked words in source files.**
- **Keep dependencies conservative** — official Microsoft or well-regarded community packages only,
  and prefer the built-in BCL over niche third-party libraries.
- **Credentials are handled carefully.** Passwords use PBKDF2-HMAC-SHA512 with a per-user random
  salt, and the iteration count and algorithm are stored alongside the hash. Nothing is ever kept in
  plaintext or in any reversible form. See [Accounts](../user-guide/accounts.md).

## Tests are required

Every behaviour change — features and bug fixes alike — is developed **test-first**, and ships with
all three test layers: unit, integration, and headless. Line coverage and the mutation score must
not drop. The [Testing](testing.md) page walks through the workflow in detail.

## Avalonia gotchas worth knowing

- **Dynamic `MenuItem` submenus** must be built in code-behind; XAML `ItemsSource` binding doesn't
  render submenus in this Avalonia version.
- **`IsVisible` on a null sub-path** evaluates `true` when the object is null — add
  `FallbackValue=False`.
- **Never replace an `ObservableCollection` instance** — mutate it in place (`Clear()` + `Add()`).
- **Compiled bindings are on by default** — every `DataTemplate` needs `x:DataType`.

## Before you open a change

- Make sure all three test layers are green and the coverage and mutation gates still pass.
- For UI changes, verify manually in the running app with explicit repro steps — see
  [Building](building.md).
