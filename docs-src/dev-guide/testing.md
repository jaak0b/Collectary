# Testing

Collectary is developed test-first. Tests are not optional and not an afterthought — they gate
every behaviour change.

## Test-driven development is mandatory

For **every** behaviour change, including bug fixes, the order is non-negotiable:

1. Write the test.
2. Run it and capture the **failing** output (the red proof).
3. Only then write the production code.
4. Re-run to green.

Writing the fix first — or "adding a test after" — is a rule violation. If production code was
edited first, revert it and restart from step 1.

## Three layers per change

Every feature and bug fix needs all three:

| Layer | Project | Base / tools |
|---|---|---|
| **Unit** | `Collectary.Core.Tests` | FakeItEasy fakes for port interfaces (`A.Fake<IXxxRepository>()`). |
| **Integration** | `Collectary.Infrastructure.Tests` | Extend `DbIntegrationTestBase` — isolated in-memory SQLite, disposed in teardown. |
| **Headless (UI)** | `Collectary.UI.Tests` | Extend `FlowTestBase` for ViewModel flows; FakeItEasy fakes for use cases. |

!!! warning "Never touch the developer's real DB or filesystem"
    Tests use in-memory SQLite (`Data Source=:memory:`) and `Path.GetTempPath()` temp dirs, disposed
    in teardown. No test reads or writes the real `%APPDATA%\Collectary` data.

## Conventions

- **Fixture name:** `<ClassUnderTest>Test` — one fixture per production class, one file per fixture.
  Never a catch-all fixture name.
- **Method name:** `MethodName_State_Expected`.
- CommunityToolkit relay commands are exercised via the generated `Command` property — e.g.
  `SaveAndGoBackAsync` → `SaveAndGoBackCommand`.

## Coverage & mutation gates

Run after every change — neither score may drop:

```powershell
.\build.ps1 --target Coverage   # line/branch coverage gate (>=95%)
.\build.ps1 --target Mutate     # mutation testing (Stryker)
```

Run a single test by filter:

```powershell
dotnet test "tests\Collectary.UI.Tests\..." --filter "FullyQualifiedName~MethodName"
```

## Manual verification too

Automated tests are required **in addition to** manual verification, never instead of it. For UI
fixes, the change is also confirmed by running the app with explicit repro steps — see
[Building](building.md).
