# Collectary

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Built with Avalonia](https://img.shields.io/badge/Built%20with-Avalonia%20%2F%20.NET-512BD4.svg)](https://avaloniaui.net)

**Collectary** is a cross-platform app for cataloguing the things you collect — books, coins, board
games, sneakers, houseplants, trading cards, whatever you track. Define each collection with typed
fields, add items, and optionally share and sync your data across devices.

It's built with [.NET](https://dotnet.microsoft.com) and [Avalonia](https://avaloniaui.net), runs on
Windows and Android, and compiles to **WebAssembly** to run in a browser tab.

🔗 **[Live demo](https://jaak0b.github.io/Collectary/demo/)** · 📖 **[Documentation](https://jaak0b.github.io/Collectary/)** · ⬇️ **[Download for Windows](https://github.com/jaak0b/Collectary/releases)**

## Highlights

- **Custom collections** — start from 20+ built-in templates (Books, Coins, Board Games, Sneakers,
  Wine, Video Games, …) or design your own.
- **22 field types** — text, rich text, numbers, currency, dates, durations, ratings, colours,
  images, tags, single/multi choice, and more.
- **Profiles** — switch between profiles from a row of tiles; no passwords, just a name.
- **Sharing & sync** — grant other profiles read/write access, and keep collections in step across
  devices through a shared folder with conflict resolution.
- **Themes & languages** — light/dark themes and an English/German UI.

## Platforms

| Platform | How to get it |
|---|---|
| **Windows** | Install `Collectary-win-Setup.exe` from the [releases page](https://github.com/jaak0b/Collectary/releases). It keeps itself up to date automatically. |
| **Android** | Sideload the APK from the [releases page](https://github.com/jaak0b/Collectary/releases) — see [Installing on Android](https://jaak0b.github.io/Collectary/user-guide/android/). |
| **Browser** | No install — open the [live demo](https://jaak0b.github.io/Collectary/demo/). |

## Build from source

You'll need the **.NET SDK 10**. To build and run the desktop app:

```powershell
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe
```

Run the test suite with `.\build.ps1 --target Test`. See the
[Building guide](https://jaak0b.github.io/Collectary/dev-guide/building/) for the full prerequisites,
the browser/Android heads, and the release tooling.

## Documentation

Full user and developer guides live at **<https://jaak0b.github.io/Collectary/>** (built from
`docs-src/` with MkDocs and published via GitHub Actions).

## License

Released under the [MIT License](LICENSE).
