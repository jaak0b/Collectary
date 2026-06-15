# Building & Running

## Prerequisites

- **.NET SDK 10** (`dotnet --version` → `10.x`). SDK 8/9 may also be installed; that's fine — the
  desktop head targets `net8.0`, but the solution (including the browser head) builds with SDK 10.
- **Workloads** (only what you actually build):
    - Desktop only → none needed.
    - **Browser (WASM) head → `dotnet workload install wasm-tools`** (see gotcha below).
    - Mobile heads → `android` / `ios`.
- Check installed workloads with `dotnet workload list`.

## Build & run

```powershell
# Desktop (primary dev target)
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe

# Browser (WASM) — needs the wasm-tools workload (see below)
dotnet run --project src\Collectary.UI.Browser
# then open the printed http://localhost:5235 / https://localhost:7169

# Tests
.\build.ps1 --target Test      # all tests (default)
.\build.ps1 --target Coverage  # coverage gate (>=95%)
.\build.ps1 --target Mutate    # mutation testing
```

There are also NUKE targets that wrap the day-to-day run/deploy flow and validate cloud credentials
up front:

```powershell
.\build.ps1 --target CheckCredentials  # fail fast if any cloud credential env var is missing
.\build.ps1 --target SetCredentials    # persist credentials for the current user (see below)
.\build.ps1 --target RunDesktop        # build + run the Windows desktop head
.\build.ps1 --target DeployAndroid     # build + install the Android head onto a connected device
.\build.ps1 --target BuildApk          # publish a signed Android APK
```

`RunDesktop`, `DeployAndroid`, and `BuildApk` all depend on `CheckCredentials`, so they refuse to
run with placeholder cloud credentials rather than producing a build that silently can't sign in.

The build project (`build/_build.csproj`) is in `Collectary.slnx` so you can edit it with full
IntelliSense, but it's flagged not-to-build for the solution's configurations — a normal
`dotnet build Collectary.slnx` (and the `Compile`/`Test`/`Coverage` targets) won't compile the
orchestrator. You still run it through `.\build.ps1`.

## Cloud credentials

OneDrive and Google Drive sign-in need real OAuth identifiers. They are **not** committed (the source
ships placeholders), and they are read from environment variables:

| Variable | Used by |
|---|---|
| `COLLECTARY_ONEDRIVE_CLIENT_ID` | OneDrive (all platforms) |
| `COLLECTARY_ANDROID_SIGNATURE_HASH` | OneDrive redirect on Android |
| `COLLECTARY_GOOGLE_CLIENT_ID` | Google Drive (Windows desktop) |
| `COLLECTARY_GOOGLE_CLIENT_SECRET` | Google Drive (Windows desktop) |

`CheckCredentials` reports each one as `ok` or `MISSING` and throws if any is absent. It looks at the
process, user, and machine scopes, so a value persisted by `SetCredentials` is found right away — and
it copies what it finds into the running build so the targets that depend on it (`RunDesktop`,
`DeployAndroid`) and the processes they launch inherit it, no shell restart required.

`SetCredentials` is interactive. Run it in a terminal and it prompts for each credential in turn:

```powershell
.\build.ps1 --target SetCredentials
```

- Input is **masked** (shown as `*`) so the value never appears on screen or in shell history.
- If you enter nothing — e.g. a paste didn't land — it shows an error and re-asks the same key
  before moving on, so you can't accidentally store a blank.
- Each value is persisted as a **per-user** environment variable (Windows `HKCU\Environment`) —
  permanent across reboots, scoped to your account, never machine-wide.

The NUKE targets pick the values up immediately (see `CheckCredentials` above). Other
already-running processes — a separate IDE, or the desktop `.exe` launched by hand — only see them
after you restart them, because Windows hands each process its environment at launch.

!!! warning "Set them in the build environment, not just a run configuration"
    These must be visible to the **build process**. A Rider *run* configuration's environment
    variables are injected only into that run — they are invisible to the build and are never
    delivered to an Android device. Set them as system/user environment variables (or in Rider's
    build environment) so both the desktop run **and** the Android build can see them.

!!! note "Android reads these at build time, desktop at runtime"
    The desktop head reads the variables from the developer's machine at runtime. The Android app
    runs on the phone, which has no access to your PC's environment, so the Android head needs the
    values baked in at build time (client id + the `BrowserTabActivity` signature hash in
    `AndroidManifest.xml`). See [Sync architecture](sync-architecture.md) for the Android sign-in
    setup.

EF Core migrations run automatically on desktop startup. To add one:

```powershell
dotnet ef migrations add <Name> --project src\Collectary.Infrastructure
```

## Versioning

Versions are computed automatically by [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
(nbgv) — there are no version numbers to bump by hand, and no version stamping in CI. The setup is two
files at the repository root:

- **`version.json`** holds the base version (`"0.1"`) and marks `master` as the public-release branch.
- **`Directory.Build.props`** adds the `Nerdbank.GitVersioning` package to every project (the version
  itself is pinned centrally in `Directory.Packages.props`). The test projects pick it up because
  `tests/Directory.Build.props` imports the root file.

From there, nbgv stamps each assembly's `AssemblyVersion`, `FileVersion`, and
`AssemblyInformationalVersion` as `0.1.<git-height>`, where the **git height** is the number of commits
in the branch's history. Every commit — and therefore every merged pull request — automatically bumps
the number, with no manual step. The Settings → About line reads that stamped version back at runtime
(`AssemblyAppVersionProvider`), trimming nbgv's `+<commit>` build-metadata suffix for display.

To start a new release line — say `0.2` or `1.0` — edit the `version` field in `version.json`; the
next commit picks it up.

!!! warning "CI must do a full clone"
    nbgv computes the version from the **whole** git history, so it fails on a shallow clone. Every
    workflow that builds checks out with `fetch-depth: 0` for this reason. A new workflow that builds
    must do the same.

## Releasing & auto-update

The Windows desktop app ships as a [Velopack](https://velopack.io/) installer that updates itself.
Velopack is wired in at two points:

- **Startup** — `Program.Main` calls `VelopackApp.Build().Run()` as its very first line (this is what
  lets Velopack finish a staged install on launch), then kicks off a silent background check via
  `UpdateCheck` + `VelopackAppUpdater`. If a newer release is found, it downloads quietly while you
  keep working, then stages the install with `WaitExitThenApplyUpdates(restart: false)` — the new
  version is swapped in *when you close the app*, so the next time you open Collectary it's already
  up to date. There is no prompt and **no forced restart mid-session**. When the app isn't a Velopack
  install (e.g. a plain `dotnet run` during development) the check is a no-op.
- **The update feed** is the project's GitHub releases (`GithubSource`), so publishing a release is
  what makes the update available to everyone.

### Cutting a release

Releases are **manual** and produced by the `Release` NUKE target, which:

1. publishes the desktop head self-contained for `win-x64`,
2. runs `vpk pack` to build `Collectary-win-Setup.exe` and the update feed (`releases.win.json`,
   the `-full.nupkg`, `RELEASES`),
3. builds the signed Android APK,
4. creates a GitHub release tagged `v<version>` whose **notes are the titles of the labeled pull
   requests merged since the previous release tag** (grouped into Features and Fixes by the `feature`
   and `fix`/`bug` labels — see [Contributing](contributing.md#pull-requests-and-release-notes)), and
   attaches the installer feed **and** the APK.

   The notes are produced by GitHub's auto-generated release notes (`gh ... releases/generate-notes`,
   driven by `.github/release.yml`). If no GitHub token is available — e.g. a local `Pack` run offline —
   it falls back to the commit messages since the previous tag so the build never breaks.

Run it from the **Actions → Release** workflow (manual dispatch). Because releases are manual, only
cut one when you've bumped the **major/minor** version in `version.json` — build-number-only changes
don't need a release. Locally you can do the same with:

```powershell
.\build.ps1 --target Release   # needs a GitHub token in GH_TOKEN/GITHUB_TOKEN and the cloud creds
```

!!! note "Repository setup the release needs"
    The release workflow builds the Android head, which bakes in the cloud OAuth identifiers, so the
    four `COLLECTARY_*` values (see *Cloud credentials* above) must exist as **repository secrets**.
    The GitHub release itself uses the automatically-provided `GITHUB_TOKEN` — no extra setup. The
    `vpk` and `nbgv` tools come from `dotnet tool restore`.

## Gotchas

### `wasm-tools` is required to build the browser head

Without it you get a runtime `System.DllNotFoundException: libSkiaSharp` (SQLite's `e_sqlite3`
fails the same way). `wasm-tools` runs the native-relink step that compiles SkiaSharp/SQLite native
code **into** the WASM bundle. It's a **build-time** workload on the dev/CI machine only — end
users need just a browser; the published output is static files with the native bits baked in.

```powershell
dotnet workload install wasm-tools   # elevated terminal
```

### The browser head is client-side WASM — there is no server

The whole app compiles to WebAssembly and runs inside the browser tab, sandboxed. `dotnet run` just
serves static files. Consequences:

- ❌ No native filesystem → the desktop's `FileSystemImageStore` and on-disk SQLite `.db` don't work.
- ✅ Browser APIs only (fetch, IndexedDB).

When `OperatingSystem.IsBrowser()`, `App.BuildContainer()` swaps in a browser infrastructure module
(EF Core **InMemory** provider + an in-memory image store + a null logger, using `EnsureCreated()`
instead of migrations). **Browser data is in-memory and resets on page refresh** — a stopgap until
a real backend exists. Desktop is unchanged (SQLite + filesystem).

### LAN access to the browser head

The default launch profile binds `localhost` only. Use the `Browser (LAN)` profile to bind all
interfaces, browse to the PC's real IP over `http` (not `https`), set the network profile to
**Private**, and open the firewall port. The full step-by-step lives in `SETUP.md` at the repo
root.

## Documentation site (this site)

The docs are built with [Material for MkDocs](https://squidfunk.github.io/mkdocs-material/):

```powershell
pip install -r requirements.txt
mkdocs serve     # live preview at http://127.0.0.1:8000
mkdocs build     # builds the static site into ./site
```

Markdown source lives in `docs-src/`; `mkdocs build` outputs into `site/` (configured via
`site_dir` in `mkdocs.yml`, and git-ignored). On `master`, once the `quality` job (full test suite)
passes, the [`docs.yml`](https://docs.github.com/en/actions) GitHub Action rebuilds the site,
publishes the WASM app into `site/app/`, and deploys the result straight to GitHub Pages with
`actions/deploy-pages`. **Nothing is committed back to the repository** — there's no generated
`docs/` folder in git any more.

!!! info "One-time GitHub setup"
    For the published site to go live, set the repository's
    **Settings → Pages → Source** to **GitHub Actions**.
