# CLAUDE.md

## Hard Rules

1. **No static methods or properties.** Exception: Avalonia `AvaloniaProperty.Register` and framework metadata only.
2. **Localization is resx-only.** All translatable strings live in `Strings.en/de.resx` or a domain-specific resx pair. Reference via `LocalizationService.Instance["Key"]` / `{Binding [Key], Source=…}`. Both language files must have every key.
3. **TDD mandatory, test-first, no exceptions.** For EVERY behavior change incl. bug fixes: commit the test before the production code. Order is non-negotiable: (a) write the test, (b) run it and PASTE the failing output, (c) only then touch production code, (d) re-run to green. A red run you can quote is the gate — no red proof = the fix does not start. Writing the fix first, or "I'll add a test after", is a rule violation; if you catch yourself having edited production code first, revert it and restart from (a).
4. **Three test layers per change.** Every feature and bug fix needs unit + integration + headless tests. "It's only a small change" is not an exemption — if it changes behavior, all three layers apply. Untestable-by-design code (pure XAML, generated code) is the only exception, and you must say so explicitly.
5. **Verification gate — local is fast, CI is thorough.** A change is NOT done until it has been verified and the real command output quoted. Never claim a feature is finished, never hand back to the user, and never commit on *assumed* results. **The full test suite and coverage are CI's job — never run them locally.** The complete `.\build.ps1 --target Test` runs on every PR via `.github/workflows/build.yml`, and the ≥95% coverage trend runs nightly via `.github/workflows/nightly.yml`; running either locally only duplicates CI and wastes dev time. What you run locally scales with the blast radius of the change:

   - **Small, localized change** (a few files inside one project, no cross-project/API/schema/DI surface touched — e.g. a single ViewModel tweak, one resx value, a catalog reorder): run only the **directly relevant test fixtures** (`dotnet test … --filter`) and quote their totals. Mutation is **not** required. State that you classified it as small and which fixtures you ran.
   - **Big or multi-file / multi-project change** (touches more than one project, or changes a public API, DB schema/migration, DI wiring, sync/backup format, or shared infrastructure): run the **local gate** below — changed-area fixtures, diff-scoped mutation, and manual UI — output quoted.

   When unsure which bucket applies, **ask the user for confirmation** before choosing — do not silently pick one. The TDD red-proof (rule #3) and three-layer thinking (rule #4) apply to *every* change regardless of size.

   Local gate:
   1. **Full suite — CI only.** Do NOT run the complete `.\build.ps1 --target Test` locally; CI runs it on every PR. Locally, run only the fixtures covering the code you touched (`dotnet test … --filter`) and paste their pass/fail totals. A failure in those fixtures blocks everything.
   2. **Coverage ≥95% — CI only.** Do NOT run `.\build.ps1 --target Coverage` locally and do not quote a local coverage number; the ≥95% line-coverage trend is tracked by the nightly CI job. Diff-scoped mutation (5.3) is what locally guarantees your changed code is actually tested.
   3. **Mutation testing — scoped to your local changes only, surviving mutants addressed. Running full Stryker is forbidden.** Stryker over the whole codebase takes far too long; never do it. Always run it scoped to your diff: `.\build.ps1 --target Mutate` `git diff`s against `HEAD` and mutates only those files, so it covers just the code you have changed since your last commit (your uncommitted working-tree changes). Run it **before you commit** — once your work is committed there is nothing left in the diff to mutate. Override the baseline only when you need a wider sweep (`.\build.ps1 --target Mutate --since <branch-or-commit>`). (`--since` is the git diff base, not Stryker's own `--since`, which LibGit2Sharp can't use in this relative-path worktree.) Stop the running Desktop app first (`Get-Process Collectary.UI.Desktop | Stop-Process -Force`) — a live instance locks `Collectary.UI.dll` and fails Stryker's build. Quote the mutation score and review survivors in the code you changed; kill them with tests or justify each explicitly.
   4. **Manual UI verification (for UI changes).** Ask the user to run the app with exact repro steps (see "Verifying UI Fixes"). Tests do not replace this; they are in addition to it.

   If a local gate cannot be completed (e.g. a pre-existing failure you did not introduce), STOP and surface it to the user with the evidence — do not quietly proceed as if it passed.
6. **No test touches the developer's DB or filesystem.** In-memory SQLite (`Data Source=:memory:`) and `Path.GetTempPath()` temp dirs, disposed in teardown.
7. **No empty catch blocks.** Log via `AppLogger.Log.Error` and surface via `IDialogService.ShowMessageAsync` for user-initiated operations.
8. **New `FieldDefinition` subtype = zero changes outside its own file.** Virtual dispatch only; one keyed Autofac registration in `UiModule`, no type-switches. **The base `FieldDefinition` is FROZEN to universal domain state only — adding a capability-, feature-, or layer-specific member to it is ILLEGAL.** Anything that only some field types care about (import, search, list-display, UI, sync, …) lives on that concern's dedicated capability interface (`ITextImportable`, `ISearchableFieldDefinition`, `IListDisplayable`, …) as a default member (`=> false` / `{ }`), overridden only by the field types that opt in; the consumer checks `field is IXxx { Member: … }`, never a member on `FieldDefinition`. If the right interface doesn't exist yet, create it — do NOT hang the member off the base class. A member belongs on `FieldDefinition` only if it is meaningful for EVERY field type without exception.
9. **Missing field type → add a simple version + on-screen note.** Never silently skip a use case.
10. **No trademarked words in files.**
11. **NuGet packages: official Microsoft or highly-regarded community only.** No niche/unmaintained single-author packages. Prefer built-in BCL APIs (e.g. PBKDF2 via `System.Security.Cryptography.Rfc2898DeriveBytes`) over third-party dependencies.
12. **Credentials are bullet-proof.** Passwords hashed with built-in PBKDF2-HMAC-SHA512, per-user random salt, iteration count + algorithm stored with the hash. Never store/log plaintext; never store anything reversible.
13. **Every new feature is documented.** Add/update the relevant `docs-src/**` page in the same change. Write in a human, conversational style — not terse machine-speak.
14. **No code comments.** Code self-explains via names and structure, in all we author (C#, XAML, YAML, JSON, `.csproj`). Banned: *what*-narration (`// build the menu`), divider banners, commented-out code (git is the history), and default XML doc-comments. Only allowed: a short non-obvious **why** the code can't express (external-bug workaround, Avalonia gotcha). Tempted to write *what*? Rename until the comment is redundant, then delete it. Markdown docs are exempt.
15. **No direct commits to `master` — every change ships as a pull request.** Committing or pushing straight to `master` is FORBIDDEN. Every change goes on its own branch and lands through a PR opened with `gh`. **The PR title must read as a release note** — a single, user-facing sentence describing the change as it would appear in the release notes (it feeds them), not an internal/implementation summary. Owner review still gates everything: never create the branch's commit, push, or open the PR until the repository owner has explicitly approved the change in chat — present the diff summary and ask, and only proceed after a clear "yes" (or equivalent). Once approved: no AI attribution in anything that touches git or GitHub — not in commit messages, PR titles or descriptions, issue/PR comments, tags, or release notes. No `Co-Authored-By` trailer, no "Generated with" line, no AI author/committer identity. This applies to every git and `gh`/GitHub API action without exception. Commits carry the human's authorship only. **Commit messages are a short, single sentence** — one line, no body, no bullet list; if a change feels too big to describe in one sentence, split it into smaller commits.
16. **No positional tuple access — code must be refactor-safe.** Never read a tuple by element position (`.Item1`/`.Item2`) and never destructure one positionally (`var (a, b) = …`). Every multi-value return is a named `record` / `record struct` whose members are read by name, so reordering or renaming a member is a compile error, not a silent value swap. This applies to return types, locals, and method results alike; a private named-element `ValueTuple` is tolerated only when it is never destructured positionally — when in doubt, declare a record.
17. **Self-review-and-fix before handoff — multi-file changes only.** Before presenting a change that touches more than one file for commit approval, run a medium-effort `/code-review` scoped to the change and **fix every finding it surfaces** — each fix following the TDD (rule #3) and verification (rule #5) rules — before the change is done. The review's own verify step already discards false positives, so any finding that reaches the list is real: it must be fixed, never merely listed. The change is NOT done while a single surfaced finding remains open. Single-file changes are exempt, mirroring the verification-gate buckets in rule #5. This is part of the Definition of Done — the owner should never have to ask for it.
18. **Shipped migrations are FROZEN — never alter an existing migration.** The app is deployed to real users with live databases, so every migration already in `src/Collectary.Infrastructure/Persistence/Migrations/` has run on someone's machine. Editing, renaming, reordering, squashing, or deleting any existing migration (or its `.Designer.cs`) is FORBIDDEN — it desyncs the `__EFMigrationsHistory` from the schema and bricks the app on startup (`Database.Migrate()` then tries to (re)create tables that already exist, exactly the crash that took the installed app down). Migrations are **append-only**: a schema change is always a brand-new migration added on top (`dotnet ef migrations add <Name>`), forward-only, that assumes the prior migrations ran verbatim. The single allowed regeneration is the auto-managed `InventoryDbContextModelSnapshot.cs`, which EF rewrites when you add a new migration. If a shipped migration is wrong, fix it with a *new* corrective migration — never by touching the old one.
19. **Release notes come from labeled PRs — label every user-facing PR.** Release notes are generated from pull-request titles by GitHub's auto-generated notes, driven by `.github/release.yml`. A PR appears in the notes **only if** it carries a category label — `feature` (→ Features) or `fix`/`bug` (→ Fixes). There is no catch-all and no opt-out label: a PR with neither label is **silently omitted**. So any change a user would notice MUST be labeled `feature` or `fix`/`bug` before merge (pairs with rule #15 — the PR title is the release-note sentence); leave deployment/CI/chore/docs-only PRs unlabeled to keep them out of the notes. Forgetting the label on a real change drops it from the release notes — treat labeling as part of opening the PR.

## Definition of Done — run this checklist before calling any change "finished"

A feature or fix is complete **only** when every box below is genuinely ticked, with real command output quoted (not assumed, not "should pass"). If you cannot tick a box, the work is not done — say so and stop.

- [ ] **Tests written first** (rule #3) — red output quoted before the production code existed.
- [ ] **All three layers present** (rule #4) — unit + integration + headless, or an explicit note on why a layer doesn't apply.
- [ ] **Tests run, scaled to the change** (rule #5) — changed-area fixtures green (`dotnet test … --filter`), totals quoted, classification stated. The full suite is CI-only — never run locally.
- [ ] **Coverage — CI only** (rule #5.2) — not a local gate; the ≥95% trend is tracked by nightly CI. Nothing to run or quote locally.
- [ ] **Mutation run scoped to local changes, survivors handled** (rule #5.3) — *big changes only*; run `.\build.ps1 --target Mutate` (diff vs `HEAD`, your uncommitted changes) **before** committing — never full Stryker; Desktop app stopped first; score quoted; new survivors killed or justified.
- [ ] **Manual UI verification requested** (rule #5.4) — for any UI change, exact repro steps handed to the user.
- [ ] **Docs updated** (rule #13).
- [ ] **Localization complete** (rule #2) — every new key in both `Strings.en.resx` and `Strings.de.resx`.
- [ ] **No code comments added** (rule #14) — re-read the diff; the only comments left are genuine non-obvious *why* notes, never *what*-narration or commented-out code.
- [ ] **Self-review run and every finding fixed** (rule #17) — multi-file change: medium `/code-review` on the diff; every surfaced finding fixed before done — none merely listed. Single-file change: state the exemption.
- [ ] **Owner review obtained** (rule #15) — diff summary presented in chat and owner has explicitly approved before any `git commit` or `git push`.
- [ ] **PR labeled for release notes** (rule #19) — a user-facing change carries `feature` or `fix`/`bug`; a deployment/CI/chore/docs-only change is intentionally left unlabeled.

Do not compress this gate to save time. "Looks done" is not done; the checklist is what makes it done.

## Build & Run

```powershell
try { Get-Process -Name "Collectary.UI.Desktop" | Stop-Process -Force } catch {}
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe

dotnet test "tests\Collectary.UI.Tests\..." --filter "FullyQualifiedName~MethodName"  # run only your changed-area fixtures locally
.\build.ps1 --target Mutate    # mutation testing — scoped to your uncommitted changes since HEAD (full runs forbidden)
.\build.ps1 --target Test      # CI ONLY (build.yml, every PR) — do not run locally
.\build.ps1 --target Coverage  # CI ONLY (nightly.yml) — do not run locally
dotnet ef migrations add <Name> --project src\Collectary.Infrastructure
```

> **Data/log location depends on build config** (`AppDataPaths.Resolve()`, `#if DEBUG`). The `%APPDATA%` paths below are **RELEASE only**:
> - **DEBUG** (what you run/debug locally): everything lives next to the build output — `src\Collectary.UI.Desktop\bin\Debug\net8.0\collectary-data\` → `collectary.db`, `images\`, `preferences.json`, and `logs\Collectary-<date>.log`. This isolates each git worktree. **When diagnosing a local run, read the log here — NOT `%APPDATA%`.** `%APPDATA%` may hold stale release logs that look current but aren't.
> - **RELEASE:** `%APPDATA%\Collectary\` → `collectary.db`, `logs\`.
>
> Migrations run on startup. A startup crash with exit 0 and "no log" almost always means you looked in `%APPDATA%` instead of the DEBUG `collectary-data\logs\`; the `[FTL]` entry is there.

## Project Structure

| Project | Role |
|---|---|
| `Collectary.Core` | Domain models, ports, use cases |
| `Collectary.Infrastructure` | EF Core SQLite, image store |
| `Collectary.Presentation` | Shared ViewModels, services, localization (Avalonia-package but no XAML) |
| `Collectary.Search` | Reusable JQL search engine (pure .NET, no UI) |
| `Collectary.Search.ViewModels` | Search VMs + `ILocalizationProvider`/`ResponsiveSearchBarLayout` (no Avalonia; mutation-tested) |
| `Collectary.Search.Avalonia` | The `SearchBar` control (XAML; mutation-excluded) |
| `Collectary.UI` | Views, DI, theming; hosts the `SearchBar` |
| `Collectary.UI.Desktop` | Desktop entry point |
| `*.Tests` | Unit (Core), Integration (Infrastructure), Headless (UI) |

## Key Patterns

**DI:** Autofac — `CoreModule`, `InfrastructureModule`, `UiModule`. ViewModels below root are not DI-registered; `MainWindowViewModel` creates them with callbacks. Use `_scope.Resolve<IXxx>()` in nav methods.

**Navigation:** callback-based — child VMs receive `Action`/`Func` at construction. `MainWindowViewModel.ContentViewModel` drives content. `ViewLocator` maps `XxxViewModel → XxxView` by convention.

**Localization:** `LocalizationService.Instance["Key"]` in C#; `{Binding [Key], Source={x:Static loc:LocalizationService.Instance}}` in XAML. `Apply(code)` switches language.

**Field editors:** `FieldEditorRegistry` and `ListCellBuilder` resolve by `definition.GetType().Name` — one keyed Autofac registration in `UiModule` per field type, no type-switch anywhere.

## Avalonia 12 Gotchas

- **Dynamic `MenuItem` submenus:** build in code-behind (`CollectionChanged` → hand-built `List<MenuItem>`). XAML `ItemsSource` binding does not render submenus in Avalonia 12.
- **`Button.Flyout` content declared in XAML never receives input:** the popup renders and its bindings resolve (headless tests pass!), but real clicks die with `(PresentationSource) PlatformImpl is null, couldn't handle input` in the log. Build flyout content in code-behind (`new Flyout()` + content controls), like the breadcrumb overflow and sync-status flyouts.
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
