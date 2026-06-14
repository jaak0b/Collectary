# Getting Started

This page covers installing and running the **desktop** version. To try the app without installing,
use the [Live Demo](../demo.md). On a phone or tablet, see [Installing on Android](android.md).

## Install & run

Collectary is a .NET 8 / Avalonia desktop app. To build and run from source:

```powershell
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe
```

Building requires the **.NET SDK 10** (the desktop head targets net8.0, but the full solution builds
with SDK 10). See [Building](../dev-guide/building.md) for the full prerequisites.

## Staying up to date

When you install Collectary from the Windows installer (`Collectary-win-Setup.exe` on the
[releases page](https://github.com/jaak0b/Collectary/releases)), it **keeps itself up to date**. On
each launch it quietly checks for a newer release in the background and downloads it; the update is
applied automatically the next time you start the app. There's nothing to click and no interruption —
you'll simply be on the latest version. You can always grab the newest installer manually from the
releases page too.

## First launch

On first launch Collectary:

1. Creates its data folder and an empty SQLite database, then runs migrations.
2. Prompts you to create the first account (see [Profiles](accounts.md)).
3. Opens the **Home** screen.

### Where your data lives

| What | Location |
|---|---|
| Database | `%APPDATA%\Collectary\collectary.db` |
| Preferences | `%APPDATA%\Collectary\preferences.json` |
| Logs | `%APPDATA%\Collectary\logs\` |

Images you attach to items are stored alongside the database.

## The main screens

| Screen | For |
|---|---|
| **Home** | Browse, create, reorder, and open collections. |
| **Collection** | View the items in one collection. |
| **Collection editor** | Define a collection's fields and groups. |
| **Item editor** | Add or edit a single item. |
| **Settings** | Theme, language, and sync. |
| **Shared Field Library** | Field definitions reused across collections. |

## Next steps

- [Create your first collection](collections.md)
- [Add items to it](items.md)
- [Learn the field types](field-types.md)
