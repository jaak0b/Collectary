# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Hard Rules

**You are forbidden from creating git commits.** Never run `git commit` under any circumstances. Only the user commits. Make changes, verify they work, then stop — do not commit.

**Adding a new `FieldDefinition` subtype must require zero changes to any class outside that subtype's own file.** Use virtual dispatch on `FieldDefinition` for type-specific behaviour (merge logic, layout defaults, etc.). A static registry or type-switch that must be updated when a new type is added violates this rule. The existing `FieldEditorRegistry` and `ListCellBuilder` keyed-registration pattern is the correct model for DI; apply the same philosophy to domain-level concerns.

**Localization is resx-only.** Every user-visible, translatable string MUST live in the resx files (`Strings.en.resx` + `Strings.de.resx`, or a domain-specific pair like `TemplateStrings.en/de.resx`) and be referenced by key — `LocalizationService.Instance["Key"]` in C#, or `{Binding [Key], Source={x:Static loc:LocalizationService.Instance}}` in XAML. Never inline translatable text in C#, XAML, or data structures. Every key added must exist in **both** language files (the German value may be a stopgap, but the key must be present). When a feature would add a large block of strings (e.g. preset templates), give it its own resx pair rather than bloating the shared `Strings.*.resx`.

**Test-Driven Development is mandatory — no exceptions.** For every bug fix or new feature, the workflow is strictly:
1. **Write a failing test first.** Add the unit/integration/headless test that captures the bug or specifies the new behaviour. Run it and confirm it **fails** for the right reason.
2. **Fix the bug or implement the feature.** Change only the production code necessary.
3. **Run the test again and confirm it passes.** Only then is the work considered done.

Never write production code before a failing test exists for it. Never write a test after the fact to rubber-stamp code that already works. This is a hard, non-negotiable rule — it applies to the smallest one-line bug fix and to the largest new feature alike.

**Every new feature and every bug fix requires unit tests, integration tests, AND headless tests — no exceptions.** A feature or fix is not done until all three layers are present:
- **Unit tests** — logic-level coverage for every new/changed class in Core and Presentation.
- **Integration tests** — repository and use-case round-trips through an in-memory SQLite DB in `Collectary.Infrastructure.Tests`.
- **Headless tests** — end-to-end ViewModel/command tests in `Collectary.UI.Tests` covering full CRUD plus all feature-specific behaviour.

**Code coverage and mutation scores must not drop.** After every change, run the full test suite including coverage and mutation analysis:
```powershell
.\build.ps1 --target Coverage   # must stay >= 95% line coverage
.\build.ps1 --target Mutate     # Stryker score must not decrease vs. the previous baseline
```
If a change causes either score to fall, add tests until both are restored before considering the work done. Never ship a feature or fix that reduces the quality bar of the test suite.

**Missing field type → stopgap + visible note.** If a feature (e.g. a preset template for a new hobby) needs a `FieldDefinition` type that doesn't exist yet, do **not** skip the use case. Add a *simple* version of the field type (following the FieldDefinition/FieldValue parallel-hierarchy rules) and surface an on-screen note so it can be refined later, rather than silently dropping the field.

## Build & Run

```powershell
# Kill running instance first (prevents file lock on DLLs)
try { Get-Process -Name "Collectary.UI.Desktop" | Stop-Process -Force } catch {}

# Build
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"

# Run
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe

# Run all tests via Nuke (preferred)
.\build.ps1 --target Test

# Run all tests directly
dotnet test "tests\Collectary.Core.Tests\Collectary.Core.Tests.csproj"
dotnet test "tests\Collectary.Infrastructure.Tests\Collectary.Infrastructure.Tests.csproj"
dotnet test "tests\Collectary.UI.Tests\Collectary.UI.Tests.csproj"

# Run a single test (by name filter)
dotnet test "tests\Collectary.Core.Tests\Collectary.Core.Tests.csproj" --filter "FullyQualifiedName~MethodName"

# Run Stryker mutation tests (requires tool restore first)
dotnet tool restore
.\build.ps1 --target Mutate
```

> **Database**: SQLite at `%APPDATA%\Collectary\collectary.db`. The app runs **EF Core migrations** on startup (`App.axaml.cs` → `db.Database.Migrate()`, with a compatibility shim that baselines a pre-existing schema-less DB). After any DbContext schema change, add a migration: `dotnet ef migrations add <Name> --project src\Collectary.Infrastructure`. Do **not** delete the DB. (Integration tests use `EnsureCreated` on in-memory SQLite, so they pick up model changes without a migration.)

## Verifying fixes

This is a desktop GUI app. When a fix needs runtime confirmation (especially UI/binding behavior that unit tests can't fully cover), **just ask the user to verify** — describe the exact repro steps to try. Do **not** attempt to drive the app via mouse/screenshot automation.

**Rule: every UI bug fix MUST ship with a headless regression/end-to-end test.** When you fix a UI bug (rendering, binding, control behavior, view resolution, persistence-as-seen-through-the-editor, etc.), add a test that fails before the fix and passes after. Use the headless Avalonia harness (`Avalonia.Headless` + `HeadlessTestApp` in `Collectary.UI.Tests`, which loads `FluentTheme` so templated controls render) for control/rendering bugs, or a ViewModel/repository round-trip test for state/persistence bugs. Asking the user to verify is in *addition* to the test, never a substitute for it.

**Rule: every new feature MUST ship with headless end-to-end tests covering full CRUD and all feature-specific options.** The developer is a single person who cannot manually verify every path after every change. Tests must stand in for that. Concrete guidance:

- **New `FieldDefinition` subtype** → add ViewModel tests that exercise all type-specific options (e.g. for a `TodoFieldDefinition` with a `HasReminder` toggle: a test that opens the preset editor, configures `HasReminder = true`, saves, and confirms the persisted definition has `HasReminder = true`; another that opens the item editor with that field and confirms the correct editor VM is created with the right default). Test both the **preset panel** (definition configuration via `FieldDefinitionRowViewModel` + `FieldEditorMapper`) and the **item panel** (value editing via the field editor ViewModel and `FieldEditorRegistry`).
- **New navigation path / view** → add a `ViewLocatorTest` entry and a smoke test that the resolved view renders without throwing.
- **New repository operation** → add an `Infrastructure.Tests` integration test that round-trips through an in-memory SQLite DB.
- **New use-case command** → add a `Core.Tests` unit test covering the happy path and at least one error/edge case.

Test scope for a new `FieldDefinition` subtype at minimum:
1. `<Type>FieldDefinitionTest` — `DefaultColumnSpan`, `ApplyTypeSpecificProperties` (copies type-specific props; ignores foreign type), `IsTitleField` (false for regular fields).
2. `<Type>FieldValueTest` — `IsEmpty`, `CopyFrom`, `ToString`.
3. `FieldEditorMapperTest` — `ToDefinition_EveryConcreteFieldType_MapsWithoutStrictModeError` already covers this reflectively; if the new type has renamed VM properties, add an explicit scalar test.
4. `<Type>FieldEditorViewModelTest` — round-trip through the item editor: construct the VM with a definition + value, exercise type-specific bindings, confirm output state.

## Project Structure

| Project | Role |
|---|---|
| `Collectary.Core` | Domain models, ports (interfaces), use cases — zero UI/infra dependencies |
| `Collectary.Infrastructure` | EF Core SQLite persistence, file-system image store |
| `Collectary.UI` | Shared Avalonia UI library: ViewModels, Views, DI modules, theming, localization, controls |
| `Collectary.UI.Desktop` | Windows desktop entry point (thin shell, just `AppBuilder`) |
| `Collectary.Core.Tests` | Unit tests for use cases (NUnit, FakeItEasy, Bogus) |
| `Collectary.Infrastructure.Tests` | Integration tests for repositories + image store (NUnit, SQLite in-memory) |
| `Collectary.UI.Tests` | Unit tests for ViewModels (NUnit, FakeItEasy, Bogus) |

## Architecture

### Dependency Injection

Autofac with three modules composed in `App.axaml.cs → OnFrameworkInitializationCompleted`:

- `CoreModule` — registers `PresetUseCase` as `IPresetUseCase`, `ItemUseCase` as `IItemUseCase`, `SystemFieldUseCase` as `ISystemFieldUseCase` (all `SingleInstance`)
- `InfrastructureModule` — registers `InventoryDbContext` (InstancePerDependency), `PresetRepository` as `IPresetRepository`, `ItemRepository` as `IItemRepository`, `SystemFieldRepository` as `ISystemFieldRepository`, `FileSystemImageStore` as `IImageStore` (all SingleInstance except DbContext)
- `UiModule` — registers `LocalizationService.Instance` and `ThemeService.Instance`, `DialogService.Instance` as `IDialogService`, `ListCellBuilder` as `IListCellBuilder`, `FieldEditorRegistry` as `IFieldEditorRegistry`, all 12 field editor VMs and list cell factories (keyed by definition type name), 4 color format editor VMs (keyed by `ColorFormat`), `ColorFormatEditorFactory`, `MainWindowViewModel`, `MainWindow`

**All DI registrations use the interface, not the concrete type.** ViewModels below the root (`HomeViewModel`, `PresetDetailViewModel`, etc.) are **not registered in DI** — `MainWindowViewModel` creates them directly and passes interface dependencies + callbacks.

When resolving use cases inside `MainWindowViewModel` navigation methods, always use `_scope.Resolve<IXxxUseCase>()`, not the concrete type.

### Ports (Core interfaces)

All interfaces live in `Collectary.Core/Ports/`:

- `IPresetRepository`, `IItemRepository`, `ISystemFieldRepository` — repository contracts
- `IImageStore` — image file persistence (SaveAsync → key, Open, DeleteAsync, Exists)
- `IPresetUseCase`, `IItemUseCase`, `ISystemFieldUseCase` — use case contracts (used for mocking in ViewModel tests)

### Navigation

`MainWindowViewModel.ContentViewModel` drives the full content area. `ViewLocator` maps `XxxViewModel → XxxView` by convention (reflection on type name).

Navigation is callback-based — each child ViewModel receives `Action`/`Func` callbacks at construction time:

```
HomeViewModel → NavigateToPreset(Preset)
             → NavigateToPresetEditor(Preset?)
PresetDetailViewModel → NavigateToItemEditor(Preset, fields, Item?)
                      → navigateBack → NavigateToHomeAsync()
ItemEditorViewModel → onSaved / onCancelled → NavigateToPreset(preset)
```

Breadcrumbs (`ObservableCollection<BreadcrumbNode>`) track the navigation stack for nested editors (list fields, list entries).

### Domain Model

`DomainObject` (base, client-generated `Guid Id`) → `Preset`, `Item`, `FieldDefinition`, `FieldValue`.

Field types follow a **parallel hierarchy**: each field has a `FieldDefinition<TValue>` subclass and a corresponding `FieldValue<TDefinition>` subclass. The 12 concrete pairs are: DisplayName, Text, Integer, Decimal, Bool, Date, Color, Rating, Url, Image, SingleChoice, MultiChoice.

`IListDisplayable` marks definition types eligible as DataGrid columns in the item list (`ShowInList` toggle per field). Image is the only type that does **not** implement it.

`PresetUseCase.GetEffectiveFieldsAsync` recursively resolves the parent preset chain and merges fields (parent fields first, then child fields). It returns an `EffectiveFields` DTO (`Fields`, `Groups`, `GroupByFieldId`) — the authoritative field list for the item editor and the item list grid (list grid uses only `.Fields`).

**Field groups** (`FieldGroup`, owned by a Preset *or* a `ListFieldDefinition`) organize fields into named sections with a `GroupDisplayMode` (Card → collapsible tile, Tab → merged tab strip; ungrouped fields render first). A field references its group via `FieldDefinition.GroupId` (per-preset/list system fields via `PresetSystemField.GroupId` / `ListSystemField.GroupId`). **Groups nest arbitrarily** via `FieldGroup.ParentGroupId` and render recursively (cards-in-cards, tab-strips-in-tab-strips) in the item editor.

- **Config**: the field-settings editor is a node tree. Both field rows (`FieldDefinitionRowViewModel`) and group nodes (`FieldGroupRowViewModel`) implement `IEditorNode` and share the master `ListBox` (`CurrentRows`). A group node is drill-in like a List field (`FieldListEditorViewModel.DrillInto` → breadcrumb level showing the group's members + sub-groups); its settings (name, display mode, `ShowInList` gate, `PrefixColumnHeaders`) edit in `Controls/GroupDetailEditor`. `EditorNodeTreeBuilder` builds/flattens the node tree ↔ flat `Groups`+`Fields`. A field's group can be reassigned via the dropdown in `FieldDetailEditor`. There is **no** separate groups panel.
- **Persistence**: `IFieldDefinitionMerger`/`FieldDefinitionMerger` (DI-registered, injected into `PresetRepository`/`SystemFieldRepository`) — `SyncGroups` add/remove/update (incl. `ParentGroupId`/`ShowInList`/`PrefixColumnHeaders`), `SyncSubFields` recurses for list groups. Deleting a group ungroups its member fields (GroupId→null, never deleted) and removes its sub-group subtree. Self-ref FK is `Restrict`.
- **Item rendering**: `FieldGroupLayout` projects editors + `FieldGroup` tree (by `ParentGroupId`) into recursive `FieldGroupViewModel.ChildRegions` + merged `TabRegionViewModel` per scope → shared `GroupedFieldsView` control (used by `ItemEditorViewModel` + `ListEntryEditorViewModel` via `IGroupedFieldHost`). Tab regions collapse to an accordion when narrow (`ItemEditingContext.IsNarrow`, set from each host view's `OnSizeChanged` at the 720px breakpoint).
- **Item list grid**: `PresetDetailViewModel` builds `ListColumns` (leaf fields only) via a depth-first walk; a group's `ShowInList=false` excludes its (and sub-groups') columns and hides the per-field "Show in list" checkbox; `PrefixColumnHeaders` prefixes the column header with the ancestor-group path (`Specs › Weight`).

System fields (`SystemField`) are globally-defined fields that can be referenced by multiple presets via `PresetSystemField` join entities. The `SystemField.Definition` property is `required`.

### Persistence

EF Core TPT (Table-Per-Type): `FieldDefinitions` base table + one sub-table per concrete type (`TextFieldDefinitions`, `ColorFieldDefinitions`, etc.). Same pattern for `FieldValues`.

Cascade delete rules:
- `Preset.Fields` → `Cascade` (deleting a preset deletes its field definitions)
- `Item.Values` → `Cascade` (deleting an item deletes its field values)
- `FieldDefinition → FieldValue` → `Cascade` (deleting a field definition deletes all values for that field)
- `Preset.ParentPresetId` → `Restrict` (cannot delete a preset that has children)

All entity IDs are `ValueGenerated.Never` (set in `ConfigureClientGeneratedKeys`).

### Field Editor / List Cell registration

`FieldEditorRegistry` (DI singleton) resolves field editor ViewModels from Autofac by name (`definition.GetType().Name`) using `NamedParameter("definition", ...)`, `NamedParameter("value", ...)`, `NamedParameter("context", ...)`. No type-switch needed — adding a new field type requires one `RegisterType<XxxFieldEditorViewModel>().Named<FieldEditorViewModelBase>(nameof(XxxFieldDefinition))` line in `UiModule`.

`ListCellBuilder` (DI singleton) uses `IIndex<string, Func<FieldValue, FieldDefinition, ListCellViewModelBase>>` keyed by definition type name — same pattern, registered in `UiModule.RegisterListCells`.

### Services

- **`IDialogService`** / **`DialogService`** — `ConfirmDeleteAsync(itemName)` and `ShowMessageAsync(message, title)`. Interface is registered in DI and injected into ViewModels. `DialogService.Instance.Owner` (Window) is set in `MainWindow` constructor. The singleton is registered as `IDialogService` in `UiModule`.
- **`LocalizationService`** — backed by `Strings.en.resx`/`Strings.de.resx`. Indexer: `LocalizationService.Instance["Key"]`. `Apply(code)` switches language and fires `LanguageChanged`.
- **`ThemeService`** — swaps `Themes/Colors.Light.axaml` or `Themes/Colors.Dark.axaml` in `Application.Resources.MergedDictionaries[0]`. All brushes use `{DynamicResource}`. Add new tokens to **both** color files.

**`AppPreferences`** — static class, Load/Save JSON at `%APPDATA%\Collectary\preferences.json`. Record fields: `Theme`, `Language`, `FieldPaneRatio`.

**`AppLogger.Log`** — routes to `Serilog.Log.Logger`. Initialized in the platform entry point. Serilog defaults to `Logger.None` when uninitialized (safe in tests). Log files at `%APPDATA%\Collectary\logs\Collectary-*.log`. Configured at `MinimumLevel.Debug`, so `Debug`/`Verbose` calls reach the file. **UI-layer code** (ViewModels, navigation) uses `AppLogger.Log.Debug(...)` directly.

### Logging (`IAppLogger`)

Core and Infrastructure cannot reference the UI's `AppLogger` (dependency rules), so they log through the **`IAppLogger`** port (`Collectary.Core/Ports/IAppLogger.cs`: `Verbose`/`Debug`/`Information`/`Warning`/`Error`). Implementations:
- `SerilogAppLogger` (UI/Services) — forwards to `AppLogger.Log`; registered as `IAppLogger` in `InfrastructureModule`.
- `NullAppLogger` (Core/Logging) — no-op default.

Use cases, repositories, and `FieldDefinitionMerger` take `IAppLogger? logger = null` as an **optional** last constructor parameter and fall back to `new NullAppLogger()`. This keeps existing `new XxxRepository(...)` / `new XxxUseCase(...)` test constructors compiling without a logger argument while DI injects the real one at runtime. Add structured `Debug` logs at meaningful seams (persistence save/sync, effective-field resolution, navigation, layout building) — prefer named template properties (`"... id={Id} count={Count}"`) over interpolation.

**Avalonia binding diagnostics** are enabled: `AvaloniaUseCompiledBindingsByDefault=true` (compile-time validation) plus `Program.cs` chains `.LogToTrace(Warning).LogToTrace(Verbose, LogArea.Binding)` and installs `AvaloniaLogSink` (Desktop) which routes Avalonia's own log events (binding failures, control warnings) into Serilog. Binding-area events appear in the log as `[Avalonia:Binding] ...`. Note: with compiled bindings, most binding errors surface at **compile time**, not runtime.

### No static methods or properties

**Static methods and properties are never allowed.** There is exactly one exception, and only because the framework API physically requires it:
- **Avalonia framework metadata** — `AvaloniaProperty.Register` (and the `AffectsMeasure`/`AffectsRender`-style registrations in a `static` constructor), `DataFormat.CreateInProcessFormat`. These must be `static readonly` / static per the Avalonia API; there is no instance alternative.

Everything else — helpers (even private/pure/stateless), factories, builders, query helpers, mappers, extension methods — must be **instance members**. If a helper feels like it "should" be static, put it as a private instance method on the relevant class or inject a collaborator. Do not reach for `static` to avoid threading an instance through.

### Error Handling

**Exception swallowing is forbidden.** Never write an empty `catch { }`. Every caught exception must:
1. Be **logged** via `AppLogger.Log.Error(ex, "...")`
2. **Inform the user** via `DialogService.Instance.ShowMessageAsync(...)` for user-initiated UI operations (ViewModel `LoadAsync`/`SaveAsync`)

For render hot paths (value converters) and best-effort infrastructure I/O (`AppPreferences`), logging alone is acceptable.

### Avalonia UI Patterns

**No comments** — the codebase is comment-free by convention.

**Compiled bindings** — `AvaloniaUseCompiledBindingsByDefault=true`. All XAML DataTemplates with a specific type need `x:DataType`.

**Avalonia 12 gotchas:**
- **Dynamic `MenuItem` submenus** — binding `MenuItem.ItemsSource` does not render in Avalonia 12. Build submenus in code-behind (`CollectionChanged` → set `ItemsSource` to a hand-built `List<MenuItem>`). See `PresetEditorView.axaml.cs → BuildSystemFieldsMenu`.
- **`IsVisible` through a null sub-path** — e.g. `IsVisible="{Binding SelectedField.IsEditable}"` evaluates to `true` when `SelectedField` is null. Always add `FallbackValue=False`.

**`ObservableCollection`s** — always mutate in place (`Clear()` + `Add()`), never replace the instance. Avalonia 12 flyout menus re-bind unreliably to a replaced collection.

**Dynamic DataGrid columns** — `PresetDetailView.axaml.cs → BuildColumns` runs when `ListFields` changes. Because `LoadAsync` is async, dispatch column building to `Dispatcher.UIThread.InvokeAsync`.

**Responsive split layout** — use `Controls/ResponsiveSplitLayout` for any new split editor (persists pane ratio via `AppPreferences.FieldPaneRatio`). See `PresetEditorView` and `SystemFieldLibraryView` for the `OnAttachedToVisualTree`/`OnDetachedFromVisualTree`/`OnSizeChanged` pattern.

**Drag-to-reorder** — use `Controls/ListReorderBehavior` (requires a drag handle control with `Tag="DragHandle"` and `DragDrop.AllowDrop="True"` on the `ListBox`). Root-level `SystemField` reorders persist immediately; nested sub-field reorders persist on Save.

## Testing

**Framework:** NUnit + FakeItEasy + Bogus (configured in `Directory.Packages.props`).

**Rule: no test ever touches the developer's database or file system paths.** Unit tests use FakeItEasy fakes. Integration tests use isolated in-memory SQLite (one `SqliteConnection("Data Source=:memory:")` per test, disposed in `[TearDown]`) and temp directories (`Path.GetTempPath()/{Guid}`).

**`Collectary.Core.Tests`** — use case tests. Mock port interfaces (`IPresetRepository`, `IItemRepository`, `ISystemFieldRepository`) with FakeItEasy. Pattern:
```csharp
_repo = A.Fake<ISystemFieldRepository>();
_sut = new SystemFieldUseCase(_repo);
A.CallTo(() => _repo.GetAllAsync()).Returns(fields);
A.CallTo(() => _repo.AddAsync(field)).MustHaveHappenedOnceExactly();
```

**Domain field type tests — one test fixture per production class, never per base type.** Each concrete field type's **definition** and **value** are separate production classes, so they get separate fixtures: `<Type>FieldDefinitionTest` (e.g. `CreateEmptyValue` returns the right `FieldValue` stamped with the definition id, type-specific defaults like `CurrencySymbol`/`DecimalPlaces`/`MaxStars`) and `<Type>FieldValueTest` (`IsEmpty`, `ToString`, `CopyFrom`), in `Collectary.Core.Tests/Domain/Fields/`. Do **not** create one catch-all `FieldValueTests`/`FieldDefinitionTests` that loops over every type — split per class so a failure names the exact type. The shared abstract-base contract (`FieldDefinition.GetOrCreateEmptyValue` new/existing/mismatch behavior) is tested once in `TextFieldDefinitionTest` as the representative type. Culture-sensitive `ToString` assertions (Currency, Percentage) must format the expected string with the same format specifier (`$"{v:F2}"`) rather than hardcoding a separator, since CI/dev locale varies.

**`Collectary.Infrastructure.Tests`** — integration tests for repository sync logic and the image store. Repository tests extend `DbIntegrationTestBase`; image store tests extend `FileSystemTestBase`. Both base classes are in `IntegrationTestBase.cs`. These tests exercise `FieldDefinitionMerger.Apply`, `SyncListEntries`, and `SyncSubValues` — paths that unit tests cannot cover without a real relational database.

**`Collectary.UI.Tests`** — ViewModel tests. Mock use case interfaces (`IPresetUseCase`, `IItemUseCase`, `ISystemFieldUseCase`) with FakeItEasy. Invoke CommunityToolkit-generated relay commands via their generated `XxxCommand` / `XxxAsyncCommand` properties (note: the toolkit strips the `Async` suffix — `SaveAndGoBackAsync` → `SaveAndGoBackCommand`).

**One test class per file:** Each `.cs` test file contains exactly one `[TestFixture]` class, and the file name must match the class name exactly (`FooTest.cs` → `class FooTest`). Non-fixture helper/harness classes that are tightly coupled to one test class may live in the same file; otherwise they get their own file (e.g. `ListFieldEditorTestHarness.cs`). A test fixture is **always** named `<ClassUnderTest>Test` — singular `Test`, never `Tests`, and never a grouping/catch-all name (`MiscViewModelTests`, `ColorAndConverterTests`, a per-base-type dump, etc.). One fixture per production class. Examples: `PresetRepository` → `PresetRepositoryTest`, `TextFieldValue` → `TextFieldValueTest`, `TextFieldDefinition` → `TextFieldDefinitionTest`, `HexColorFormatEditorViewModel` → `HexColorFormatEditorViewModelTest`.

**Method naming:** `MethodName_StateUnderTest_ExpectedBehavior`

**What is not tested:** Views (`.axaml`), `MainWindowViewModel` (requires full Autofac container + Avalonia runtime), platform entry points. `PresetDetailViewModel` uses `IListCellBuilder` (mockable). `ItemEditorViewModel` uses `IFieldEditorRegistry` via `ItemEditingContext` (mockable). Both are tested.

### Nuke Build Targets

`build/Build.cs` defines the build pipeline. Run via `.\build.ps1` from the repo root.

| Target | Action |
|---|---|
| `Restore` | `dotnet restore` on the solution |
| `Compile` | `dotnet build` (depends on Restore) |
| `Test` | Runs all 3 test projects sequentially (depends on Compile) |
| `Coverage` | Runs all 3 test projects with `--collect:"XPlat Code Coverage" --settings coverlet.runsettings`, merges via ReportGenerator, and **fails the build if merged line coverage < 95%** (`--CoverageThreshold` parameter overrides the default). Depends on Compile. |
| `Mutate` | Runs Stryker on Core + Infrastructure + UI (depends on Test) |

Default target is `Test`.

**Coverage gate** (`Coverage` target): `coverlet.runsettings` at the repo root drives exclusions and `SkipAutoProps`. Four categories are excluded from the coverage denominator because they have no unit-testable logic: (1) EF Core migrations + `InventoryDbContext` scaffolding, (2) Avalonia Views / custom-control code-behind / generated XAML / `App` / `ViewLocator`, (3) application-shell pieces that require a running Avalonia app — `MainWindowViewModel`, `DialogService`, `ThemeService`, `AppLogger` (same set as `stryker-config.json`), and (4) DI modules. The excluded Presentation classes must use the actual assembly + full namespace (e.g. `[Collectary.Presentation]Collectary.Presentation.ViewModels.MainWindowViewModel`). Test assemblies (`[*.Tests]*`) are also excluded so the number reflects production code only. Merged report (incl. HTML) lands in `TestResults/CoverageReport/`.

**Stryker** (`Mutate` target): config in `stryker-config.json` — `mutation-level: Advanced`, `thresholds` `{ high: 95, low: 75, break: 0 }` (note Stryker 4.x requires the nested `thresholds` object, not flat `threshold-*` keys). Output lands in `StrykerOutput/` (gitignored); HTML report at `StrykerOutput/reports/mutation-report.html`.

> **First-time setup**: run `dotnet tool restore` to install Stryker + Nuke local tools from `.config/dotnet-tools.json`.
