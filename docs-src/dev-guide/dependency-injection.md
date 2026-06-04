# Dependency Injection & Navigation

## Autofac modules

Collectary composes its object graph with [Autofac](https://autofac.org), split into three modules
registered at startup:

| Module | Registers |
|---|---|
| `CoreModule` | Use cases and domain services — e.g. `IPresetUseCase`, `IItemUseCase`, `ISystemFieldUseCase`, `IShareUseCase`, collection authorization, account bootstrap. |
| `InfrastructureModule` | Adapters — repositories (`IPresetRepository`, `IItemRepository`, `IUserRepository`, …), `IImageStore`, the sync stack (`ISyncService`, `ISyncBackend`, `ISyncStore`, `ISyncSerializer`), credential store/hasher, logger. |
| `UiModule` | Localization, theming, dialogs, the field-editor and list-cell registries (one keyed registration per field type), discovered preset templates, `MainWindowViewModel`, and the sync scheduler. |

The browser head substitutes a browser-specific infrastructure module (in-memory EF + image store)
in place of `InfrastructureModule` — see [Building](building.md).

## Resolving services

Inside navigation methods, resolve services from the current scope:

```csharp
var useCase = _scope.Resolve<IPresetUseCase>();
```

## Navigation is callback-based

There is **no global navigation service**. Instead:

- `MainWindowViewModel` owns a `ContentViewModel` property; whatever it points to is what's shown.
- ViewModels **below the root are not DI-registered**. `MainWindowViewModel` constructs them
  directly and passes `Action`/`Func` callbacks for navigating onward (e.g. "open this collection",
  "go back").
- `ViewLocator` maps a ViewModel to its View by naming convention: `XxxViewModel` → `XxxView`.

This keeps child ViewModels ignorant of their place in the navigation tree — they just invoke the
callbacks they were handed.

## Field editors & list cells

`FieldEditorRegistry` and `ListCellBuilder` resolve the right editor/renderer for a field by
`definition.GetType().Name`. Each field type has **one keyed Autofac registration** in `UiModule`
(keyed by the type name) and there are **no type-switches** anywhere. Adding a new field type is
therefore a localized change — see [Adding a field type](adding-a-field-type.md).
