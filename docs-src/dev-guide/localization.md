# Localization

Collectary's UI is fully localized (English and German), and **all translatable strings live in
resx resource files** — never hard-coded in C# or XAML.

## The rule

> Every translatable string lives in `Strings.en.resx` / `Strings.de.resx` (or a domain-specific
> resx pair). **Both language files must contain every key.** A key present in one but missing in
> the other is a bug.

## Using strings in C#

```csharp
var title = LocalizationService.Instance["HomeTitle"];
```

`LocalizationService` is a singleton accessed via `LocalizationService.Instance`. It exposes an
indexer that looks up a key in the currently active language.

## Using strings in XAML

```xml
<TextBlock Text="{Binding [HomeTitle],
                  Source={x:Static loc:LocalizationService.Instance}}" />
```

The indexer binding resolves the key against the active language and updates live when the language
changes.

## Switching language

```csharp
LocalizationService.Instance.Apply("de");   // or "en"
```

`Apply` swaps the active language and raises a change notification, so bound UI updates immediately
without a restart. The chosen language is persisted in preferences (see [Settings](../user-guide/settings.md)).

## Adding a new string

1. Add the key with its English value to `Strings.en.resx`.
2. Add the **same key** with its German value to `Strings.de.resx`.
3. Reference it via the indexer in C# or XAML as above.

!!! warning "Keep both files in sync"
    Because both resx files must have every key, always add a key to *both* language files in the
    same change. Missing keys are treated as defects.
