# Contributing

Collectary has a small set of **hard rules** that every change must respect. They're summarized
here; the authoritative copy is `CLAUDE.md` at the repo root.

## Hard rules

1. **No automatic commits.** Only the repository owner commits.
2. **No static methods or properties** — except Avalonia `AvaloniaProperty.Register` and framework
   metadata.
3. **Localization is resx-only.** All translatable strings live in `Strings.en/de.resx` (or a
   domain-specific resx pair), referenced via `LocalizationService`. Both languages must have every
   key. See [Localization](localization.md).
4. **TDD is mandatory and test-first.** Red proof before production code, every time. See
   [Testing](testing.md).
5. **Three test layers per change** — unit + integration + headless.
6. **Coverage (≥95%) and mutation scores must not drop.**
7. **No test touches the developer's DB or filesystem** — in-memory SQLite and temp dirs only.
8. **No empty catch blocks.** Log via the app logger and surface user-facing failures through the
   dialog service.
9. **A new `FieldDefinition` subtype changes nothing outside its own files** — virtual dispatch,
   one keyed registration. See [Adding a field type](adding-a-field-type.md).
10. **Missing field type → ship a simple version plus an on-screen note.** Never silently skip a
    use case.
11. **No trademarked words in files.**
12. **NuGet packages: official Microsoft or highly-regarded community only.** Prefer built-in BCL
    APIs over niche third-party dependencies.
13. **Credentials are bullet-proof** — PBKDF2-HMAC-SHA512, per-user random salt, iteration count +
    algorithm stored with the hash; never plaintext, never reversible. See
    [Accounts](../user-guide/accounts.md).

## Avalonia gotchas worth knowing

- **Dynamic `MenuItem` submenus** must be built in code-behind; XAML `ItemsSource` binding doesn't
  render submenus in this Avalonia version.
- **`IsVisible` on a null sub-path** evaluates `true` when the object is null — add
  `FallbackValue=False`.
- **Never replace an `ObservableCollection` instance** — mutate in place (`Clear()` + `Add()`).
- **Compiled bindings are on by default** — every `DataTemplate` needs `x:DataType`.

## Before you open a change

- Read `CLAUDE.md` in full.
- Make sure the three test layers are green and the coverage/mutation gates still pass.
- For UI changes, verify manually in the running app with explicit repro steps.
