# CLAUDE.md

## Hard Rules

1. **No git commits.** Never run `git commit`. Only the user commits.
2. **No static methods or properties.** Exception: Avalonia `AvaloniaProperty.Register` and framework metadata only.
3. **Localization is resx-only.** All translatable strings live in `Strings.en/de.resx` or a domain-specific resx pair. Reference via `LocalizationService.Instance["Key"]` / `{Binding [Key], Source=…}`. Both language files must have every key.
4. **TDD mandatory, test-first, no exceptions.** For EVERY behavior change incl. bug fixes: commit the test before the production code. Order is non-negotiable: (a) write the test, (b) run it and PASTE the failing output, (c) only then touch production code, (d) re-run to green. A red run you can quote is the gate — no red proof = the fix does not start. Writing the fix first, or "I'll add a test after", is a rule violation; if you catch yourself having edited production code first, revert it and restart from (a).
5. **Three test layers per change.** Every feature and bug fix needs unit + integration + headless tests. "It's only a small change" is not an exemption — if it changes behavior, all three layers apply. Untestable-by-design code (pure XAML, generated code) is the only exception, and you must say so explicitly.
6. **Verification gate — mandatory, every change, no exceptions.** A change is NOT done until all four steps below have actually been run and their real output quoted. Never claim a feature is finished, never hand back to the user, and (when authorized) never commit until this whole gate is green. Skipping, deferring ("I'll run it after"), or *assuming* a result is a rule violation.
   1. **Full suite green.** Run the complete `.\build.ps1 --target Test` (not just the fixtures you touched) and paste the pass/fail totals. A single failure blocks everything.
   2. **Coverage ≥95% and not dropped.** Run `.\build.ps1 --target Coverage`, quote the exact merged line-coverage number. If it dropped versus the baseline — even while still ≥95% — that is a regression: add tests until it recovers, or state precisely why (e.g. pre-existing untested code in an unrelated assembly) with the measured baseline to prove it.
   3. **Mutation testing run and surviving mutants addressed.** Run `.\build.ps1 --target Mutate`. Stop the running Desktop app first (`Get-Process Collectary.UI.Desktop | Stop-Process -Force`) — a live instance locks `Collectary.UI.dll` and fails Stryker's build. Quote the mutation score and review survivors in the code you changed; kill them with tests or justify each explicitly.
   4. **Manual UI verification (for UI changes).** Ask the user to run the app with exact repro steps (see "Verifying UI Fixes"). Tests do not replace this; they are in addition to it.

   If any gate cannot be completed (e.g. a pre-existing failure you did not introduce), STOP and surface it to the user with the evidence — do not quietly proceed as if it passed.
7. **No test touches the developer's DB or filesystem.** In-memory SQLite (`Data Source=:memory:`) and `Path.GetTempPath()` temp dirs, disposed in teardown.
8. **No empty catch blocks.** Log via `AppLogger.Log.Error` and surface via `IDialogService.ShowMessageAsync` for user-initiated operations.
9. **New `FieldDefinition` subtype = zero changes outside its own file.** Virtual dispatch only; one keyed Autofac registration in `UiModule`, no type-switches.
10. **Missing field type → add a simple version + on-screen note.** Never silently skip a use case.
11. **No trademarked words in files.**
12. **NuGet packages: official Microsoft or highly-regarded community only.** No niche/unmaintained single-author packages. Prefer built-in BCL APIs (e.g. PBKDF2 via `System.Security.Cryptography.Rfc2898DeriveBytes`) over third-party dependencies.
13. **Credentials are bullet-proof.** Passwords hashed with built-in PBKDF2-HMAC-SHA512, per-user random salt, iteration count + algorithm stored with the hash. Never store/log plaintext; never store anything reversible.
14. **Every new feature is documented.** Add/update the relevant `docs-src/**` page in the same change. Write in a human, conversational style — not terse machine-speak.

## Definition of Done — run this checklist before calling any change "finished"

A feature or fix is complete **only** when every box below is genuinely ticked, with real command output quoted (not assumed, not "should pass"). If you cannot tick a box, the work is not done — say so and stop.

- [ ] **Tests written first** (rule #4) — red output quoted before the production code existed.
- [ ] **All three layers present** (rule #5) — unit + integration + headless, or an explicit note on why a layer doesn't apply.
- [ ] **Full test suite green** (rule #6.1) — `.\build.ps1 --target Test`, totals quoted.
- [ ] **Coverage ≥95% and not dropped** (rule #6.2) — exact number quoted; regressions explained with a measured baseline.
- [ ] **Mutation run, survivors handled** (rule #6.3) — Desktop app stopped first; score quoted; new survivors killed or justified.
- [ ] **Manual UI verification requested** (rule #6.4) — for any UI change, exact repro steps handed to the user.
- [ ] **Docs updated** (rule #14).
- [ ] **Localization complete** (rule #3) — every new key in both `Strings.en.resx` and `Strings.de.resx`.

Do not compress this gate to save time. "Looks done" is not done; the checklist is what makes it done.

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
