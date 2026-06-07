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

## Cloud credentials

OneDrive and Google Drive sign-in need real OAuth identifiers. They are **not** committed (the source
ships placeholders), and they are read from environment variables:

| Variable | Used by |
|---|---|
| `COLLECTARY_ONEDRIVE_CLIENT_ID` | OneDrive (all platforms) |
| `COLLECTARY_ANDROID_SIGNATURE_HASH` | OneDrive redirect on Android |
| `COLLECTARY_GOOGLE_CLIENT_ID` | Google Drive (Windows desktop) |
| `COLLECTARY_GOOGLE_CLIENT_SECRET` | Google Drive (Windows desktop) |

`CheckCredentials` reports each one as `ok` or `MISSING` and throws if any is absent.

`SetCredentials` is interactive. Run it in a terminal and it prompts for each credential in turn:

```powershell
.\build.ps1 --target SetCredentials
```

- Input is **masked** (shown as `*`) so the value never appears on screen or in shell history.
- If you enter nothing — e.g. a paste didn't land — it shows an error and re-asks the same key
  before moving on, so you can't accidentally store a blank.
- Each value is persisted as a **per-user** environment variable (Windows `HKCU\Environment`) —
  permanent across reboots, scoped to your account, never machine-wide.

Restart any running terminals/IDEs afterwards to pick up the new values.

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
mkdocs build     # builds the static site into ./docs
```

Markdown source lives in `docs-src/`; `mkdocs build` outputs into `docs/` (configured via
`site_dir` in `mkdocs.yml`). On `master`, once the `quality` job (build + tests + format) passes, the
[`docs.yml`](https://docs.github.com/en/actions) GitHub Action rebuilds the site, publishes the
WASM app into `docs/app/`, and commits `docs/` back to the branch.

!!! info "One-time GitHub setup"
    For the published site to go live, set the repository's
    **Settings → Pages → Source** to **Deploy from a branch**, branch **`master`**, folder **`/docs`**.
