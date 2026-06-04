# CLAUDE.md

## Hard Rules

1. **No git commits.** Never run `git commit`. Only the user commits.
2. **No static methods or properties.** Exception: Avalonia `AvaloniaProperty.Register` and framework metadata only.
3. **Localization is resx-only.** All translatable strings live in `Strings.en/de.resx` or a domain-specific resx pair. Reference via `LocalizationService.Instance["Key"]` / `{Binding [Key], Source=…}`. Both language files must have every key.
4. **TDD mandatory.** Write a failing test → fix → confirm pass. No exceptions, no rubber-stamping.
5. **Three test layers per change.** Every feature and bug fix needs unit + integration + headless tests.
6. **Coverage and mutation scores must not drop.** `.\build.ps1 --target Coverage` (≥95%) and `.\build.ps1 --target Mutate` after every change.
7. **No test touches the developer's DB or filesystem.** In-memory SQLite (`Data Source=:memory:`) and `Path.GetTempPath()` temp dirs, disposed in teardown.
8. **No empty catch blocks.** Log via `AppLogger.Log.Error` and surface via `IDialogService.ShowMessageAsync` for user-initiated operations.
9. **New `FieldDefinition` subtype = zero changes outside its own file.** Virtual dispatch only; one keyed Autofac registration in `UiModule`, no type-switches.
10. **Missing field type → add a simple version + on-screen note.** Never silently skip a use case.
11. **No trademarked words in files.**
12. **NuGet packages: official Microsoft or highly-regarded community only.** No niche/unmaintained single-author packages. Prefer built-in BCL APIs (e.g. PBKDF2 via `System.Security.Cryptography.Rfc2898DeriveBytes`) over third-party dependencies.
13. **Credentials are bullet-proof.** Passwords hashed with built-in PBKDF2-HMAC-SHA512, per-user random salt, iteration count + algorithm stored with the hash. Never store/log plaintext; never store anything reversible.

## Build & Run

```powershell
try { Get-Process -Name "Collectary.UI.Desktop" | Stop-Process -Force } catch {}
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe

.\build.ps1 --target Test      # all tests (default)
.\build.ps1 --target Coverage  # coverage gate ≥95%
.\build.ps1 --target Mutate    # mutation testing
dotnet test "tests\Collectary.UI.Tests\..." --filter "FullyQualifiedName~MethodName"
dotnet ef migrations add <Name> --project src\Collectary.Infrastructure
```

> DB: `%APPDATA%\Collectary\collectary.db` — migrations run on startup. Logs: `%APPDATA%\Collectary\logs\`.

## Project Structure

| Project | Role |
|---|---|
| `Collectary.Core` | Domain models, ports, use cases |
| `Collectary.Infrastructure` | EF Core SQLite, image store |
| `Collectary.UI` | ViewModels, Views, DI, localization, theming |
| `Collectary.UI.Desktop` | Desktop entry point |
| `*.Tests` | Unit (Core), Integration (Infrastructure), Headless (UI) |

## Key Patterns

**DI:** Autofac — `CoreModule`, `InfrastructureModule`, `UiModule`. ViewModels below root are not DI-registered; `MainWindowViewModel` creates them with callbacks. Use `_scope.Resolve<IXxx>()` in nav methods.

**Navigation:** callback-based — child VMs receive `Action`/`Func` at construction. `MainWindowViewModel.ContentViewModel` drives content. `ViewLocator` maps `XxxViewModel → XxxView` by convention.

**Localization:** `LocalizationService.Instance["Key"]` in C#; `{Binding [Key], Source={x:Static loc:LocalizationService.Instance}}` in XAML. `Apply(code)` switches language.

**Field editors:** `FieldEditorRegistry` and `ListCellBuilder` resolve by `definition.GetType().Name` — one keyed Autofac registration in `UiModule` per field type, no type-switch anywhere.

## Avalonia 12 Gotchas

- **Dynamic `MenuItem` submenus:** build in code-behind (`CollectionChanged` → hand-built `List<MenuItem>`). XAML `ItemsSource` binding does not render submenus in Avalonia 12.
- **`IsVisible` on a null sub-path** evaluates `true` when the object is null — always add `FallbackValue=False`.
- **Never replace an `ObservableCollection` instance** — mutate in place (`Clear()` + `Add()`). Flyout menus re-bind unreliably to a replaced collection.
- **Compiled bindings:** `AvaloniaUseCompiledBindingsByDefault=true`. All `DataTemplate`s need `x:DataType`.

## Testing

**Conventions:** fixture = `<ClassUnderTest>Test`, method = `MethodName_State_Expected`. One fixture per production class, one file per fixture. Never a catch-all fixture name.

**Core tests:** FakeItEasy fakes for port interfaces (`A.Fake<IXxxRepository>()`).

**Infrastructure tests:** extend `DbIntegrationTestBase` (isolated in-memory SQLite, disposed in teardown).

**UI tests:** extend `FlowTestBase` for ViewModel flows; FakeItEasy fakes for use case interfaces. CommunityToolkit relay commands via generated property — `SaveAndGoBackAsync` → `SaveAndGoBackCommand`.

**Minimum for a new `FieldDefinition` subtype:** `<Type>FieldDefinitionTest`, `<Type>FieldValueTest`, `FieldEditorMapperTest` entry, `<Type>FieldEditorViewModelTest`.

## Verifying UI Fixes

Ask the user to run the app with exact repro steps. Do not automate. Tests are required *in addition* to manual verification, never instead.
