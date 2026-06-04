# Getting Started

This page covers installing and launching the **desktop** version of Collectary. If you just want
to try the app without installing anything, use the [Live Demo](../demo.md) instead.

## Install & run

Collectary is a .NET 8 / Avalonia desktop application. To build and run it from source:

```powershell
# Build the desktop head
dotnet build "src\Collectary.UI.Desktop\Collectary.UI.Desktop.csproj"

# Launch it
.\src\Collectary.UI.Desktop\bin\Debug\net8.0\Collectary.UI.Desktop.exe
```

Building from source requires the **.NET SDK 10** (the desktop head targets net8.0, but the
solution as a whole — including the browser head — uses SDK 10). See the developer
[Building](../dev-guide/building.md) guide for the full prerequisites.

## First launch

On first launch Collectary:

1. Creates its data folder and an empty SQLite database, then runs migrations automatically.
2. Prompts you to create the first user account (see [Accounts](accounts.md)).
3. Opens the **Home** screen, where your collections live.

### Where your data lives

| What | Location |
|---|---|
| Database | `%APPDATA%\Collectary\collectary.db` |
| Preferences | `%APPDATA%\Collectary\preferences.json` |
| Logs | `%APPDATA%\Collectary\logs\` |

Images you attach to items are stored alongside the database in the app's data folder.

## The main screens

| Screen | What it's for |
|---|---|
| **Home** | Browse, create, reorder, and open your collections. |
| **Collection** | View the items inside one collection. |
| **Collection editor** | Define a collection's fields and field groups. |
| **Item editor** | Add or edit a single item. |
| **Settings** | Theme, language, and sync configuration. |
| **System Field Library** | Reusable field definitions shared across collections. |

## Next steps

- [Create your first collection](collections.md)
- [Add items to it](items.md)
- [Learn the field types](field-types.md)
