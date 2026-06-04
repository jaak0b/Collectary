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
`site_dir` in `mkdocs.yml`). On `main`, the
[`docs.yml`](https://docs.github.com/en/actions) GitHub Action rebuilds the site, publishes the
WASM app into `docs/app/`, and commits `docs/` back to the branch.

!!! info "One-time GitHub setup"
    For the published site to go live, set the repository's
    **Settings → Pages → Source** to **Deploy from a branch**, branch **`main`**, folder **`/docs`**.
