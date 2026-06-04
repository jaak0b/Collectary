# Setup & Gotchas

A living doc for dev-machine setup and the non-obvious things that bite us.
Keep it short. Add an entry the moment something costs you more than 10 minutes.

## Prerequisites

- **.NET SDK 10** (`dotnet --version` → `10.x`). SDK 8/9 may also be installed; that's fine.
- **Workloads** (only what you actually build):
  - Desktop only → none needed.
  - **Browser (WASM) head → `dotnet workload install wasm-tools`** (see gotcha below).
  - Mobile heads → `android` / `ios` (already present via Visual Studio install).
- Check installed workloads: `dotnet workload list`.

## Build & Run

```powershell
# Desktop (primary dev target)
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe

# Browser (WASM) — needs wasm-tools (see gotchas)
dotnet run --project src\Collectary.UI.Browser
# then open the printed http://localhost:5235 / https://localhost:7169

# Tests
dotnet test
```

## Gotchas

### `wasm-tools` workload is required to build the Browser head
Without it you get a runtime `System.DllNotFoundException: libSkiaSharp` (and SQLite's
`e_sqlite3` would fail the same way). `wasm-tools` runs the native-relink step that compiles
SkiaSharp/SQLite native code **into** the WASM bundle.
- **Build-time only.** It's a .NET SDK workload on the *dev/CI* machine. End users need only a
  browser; the published output is static files with the native bits baked in.
- Fix: `dotnet workload install wasm-tools` (elevated terminal).

### The Browser head is client-side WASM — there is no server
The whole app compiles to WebAssembly and runs **inside the browser tab**, sandboxed.
`dotnet run` just serves static files; your C# does **not** run as a process on your PC.
Consequences in the browser:
- ❌ No native filesystem → `FileSystemImageStore` and the local SQLite `.db` file do not work.
- ❌ No native libs unless compiled to WASM (SkiaSharp, SQLite).
- ✅ Browser APIs only (HTTP/fetch, IndexedDB).

To get browser access with real data you need a backend (an ASP.NET Core `Collectary.Server`
that owns the DB + images) which the WASM client calls over HTTP. "A server on my PC with the
SQLite file" = that backend, **not** the standalone Browser head.

**Current browser storage = in-memory, non-persistent.** When `OperatingSystem.IsBrowser()`,
`App.BuildContainer()` swaps in `BrowserInfrastructureModule` (EF Core **InMemory** provider +
`InMemoryImageStore`, `NullAppLogger`, `EnsureCreated()` instead of migrations). Data and images
**reset on page refresh** — this is a stopgap so the UI renders in the browser until a real
backend exists. Desktop is unchanged (SQLite + filesystem).

### Accessing the Browser head from another device (LAN)
The default profile binds **localhost only**, so your PC's IP / other devices can't reach it.
Getting LAN access right needs all of the following — we hit every one of these in order:

**1. Run the `Browser (LAN)` profile — NOT the default.**
The default profile's `applicationUrl` is `https://localhost:7169;http://localhost:5235` →
it binds `127.0.0.1`/`::1` only. The `Browser (LAN)` profile binds `http://0.0.0.0:5235`
(all interfaces, `launchBrowser:false`).
```powershell
dotnet run --project src\Collectary.UI.Browser --launch-profile "Browser (LAN)"
```
In Rider: pick **Browser (LAN)** in the run-config dropdown (not Collectary.UI.Browser).

**2. Don't browse to `0.0.0.0`.** That's a *bind* address ("all interfaces"), not a destination.
The console line `Now listening on: http://0.0.0.0:5235` means "reachable on every IP of this PC".
Open one of the real addresses instead:
- this PC: `http://localhost:5235` or `http://<your-pc-ip>:5235`
- another device: `http://<your-pc-ip>:5235`

**3. Always `http`, never `https`.** The LAN binding is plain http; the dev HTTPS cert only
matches `localhost`, so `https://<ip>` always fails.

**4. Network must be `Private`.** Wi-Fi often defaults to `Public`, which blocks the rule below.
Check with `Get-NetConnectionProfile`; if Public, fix in **Administrator PowerShell**:
```powershell
Set-NetConnectionProfile -InterfaceAlias Wi-Fi -NetworkCategory Private
```

**5. Open the firewall port once** (Administrator PowerShell; single line, space-free name so
pasted smart-quotes can't break it):
```powershell
New-NetFirewallRule -DisplayName Collectary-Browser-5235 -Direction Inbound -Action Allow -Protocol TCP -LocalPort 5235 -Profile Private
```
cmd.exe equivalent (also Administrator):
```cmd
netsh advfirewall firewall add rule name=Collectary-Browser-5235 dir=in action=allow protocol=TCP localport=5235 profile=private
```
(or just click **Allow** on the first-run Windows Firewall popup, ticking **Private**).

**Verify the binding** (should show `0.0.0.0`, not `127.0.0.1`):
```powershell
Get-NetTCPConnection -LocalPort 5235 -State Listen | Select-Object LocalAddress,LocalPort
```

**"Address already in use"** = a leftover dev server (only one can hold the port). Kill it:
```powershell
Get-NetTCPConnection -LocalPort 5235 -State Listen | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

`http://<ip>` is **not a secure context**, so some browser APIs may be limited. For real remote
access, publish the static output and host it behind HTTPS.

### Rider: "Default system browser option is not supported for Blazor apps"
Because the Browser project uses the WebAssembly SDK + `inspectUri`, Rider treats it like Blazor
and refuses the "Default" browser.
- Fix: Run → Edit Configurations → Collectary.UI.Browser → set **Browser** to Chrome/Edge
  (not "Default"). Or run from the terminal with `dotnet run`.

### Solution didn't show the Browser/iOS projects
`Collectary.slnx` was missing the Browser and iOS heads (the `.csproj` files existed on disk).
They're added now; if a head goes missing from Rider, check it's listed in `Collectary.slnx`.
