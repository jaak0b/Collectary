# Architecture

Collectary follows a layered, ports-and-adapters (hexagonal) architecture. Dependencies point
inward: the UI depends on the application/presentation layer, which depends on the domain, which
depends on nothing.

## Projects

| Project | Layer | Responsibility |
|---|---|---|
| `Collectary.Core` | Domain & use cases | Domain models, **ports** (interfaces), use cases, auth logic. No framework dependencies. |
| `Collectary.Infrastructure` | Adapters | EF Core SQLite persistence, image store, sync backend, credential hashing — concrete implementations of Core's ports. |
| `Collectary.Presentation` | Application / MVVM | ViewModels, localization, theming, preset templates, field-editor view models. |
| `Collectary.UI` | UI framework | Avalonia XAML views, DI composition, dialogs, controls. |
| `Collectary.UI.Desktop` | Entry point | Desktop (WinExe) host. |
| `Collectary.UI.Browser` | Entry point | WebAssembly host — the [Live Demo](../demo.md). |
| `Collectary.UI.Android` / `.iOS` | Entry point | Mobile hosts. |
| `*.Tests` | Tests | Unit (Core), Integration (Infrastructure), Headless (UI). |

## The dependency rule

`Core` defines interfaces ("ports") such as `IPresetRepository`, `IImageStore`, `ISyncBackend`,
and `ICredentialHasher`. `Infrastructure` provides the concrete adapters. The application is wired
together at startup via [dependency injection](dependency-injection.md), so the domain never
references EF Core, the filesystem, or Avalonia directly.

This is what lets the **browser head** swap the desktop adapters (SQLite + filesystem) for
in-memory ones without touching the domain or the ViewModels — see [Building](building.md).

## Key cross-cutting systems

- **Field definitions** — a polymorphic type hierarchy (`FieldDefinition` and ~22 subtypes) drives
  both data and UI. Editors and list-cell renderers are resolved by type name, with no
  type-switches anywhere. See [Adding a field type](adding-a-field-type.md).
- **Localization** — resx-only, accessed through a single `LocalizationService`. See
  [Localization](localization.md).
- **Sync** — `ISyncService` / `ISyncBackend` / `ISyncStore` reconcile local and remote state. See
  [Sync architecture](sync-architecture.md).

## Navigation

Navigation is **callback-based** rather than going through a global navigation service.
`MainWindowViewModel` exposes a `ContentViewModel` that drives what's on screen. Child ViewModels
below the root are not registered in DI; `MainWindowViewModel` constructs them and hands them
`Action`/`Func` callbacks for navigating onward. `ViewLocator` maps `XxxViewModel` → `XxxView` by
convention. More in [Dependency Injection](dependency-injection.md).
