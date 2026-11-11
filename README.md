# DevBar for Windows

Windows port of [boxed/DevBar](https://github.com/boxed/DevBar) — polls a JSON endpoint and shows developer workflow status as a row of emoji pills on your taskbar, like the macOS menu bar version.

## Features

- **Taskbar overlay** — one pill per category that has items (its emoji plus a count), docked at the left edge of every taskbar. Hides while the taskbar is auto-hidden or a fullscreen app is in front.
- **Color-coded pills** — red (critical), amber (warning), blue (in progress), green (good). Categories without a built-in status are colored by priority: below 10 is red, 10–12 is amber, anything else uses the normal text color.
- **Click a pill** — opens a popup listing the items per category; click an item to open its URL.
- **New-item bounce** — a pill bounces when its category gains an item.
- **Right-click a pill** — Preferences, Refresh, Quit.
- **Follows the taskbar theme** — light or dark.
- **Battery-aware polling** — throttles to 30 s intervals on battery power.
- **503 handling** — keeps showing old data while the server warms up. A `—` pill means the server can't be reached.

## Setup

1. Download `DevBar.exe` from [Releases](https://github.com/axelf777/DevBar-Windows/releases). It's self-contained — no .NET install needed.
2. Run it. The exe isn't code-signed, so Windows SmartScreen may warn on first run: click **More info → Run anyway**.
3. A Preferences window opens on first launch. Enter your DevBar JSON endpoint URL, optionally tick **Start with Windows**, and click Save.

Settings live in `%AppData%\DevBar\settings.json`.

## Troubleshooting

DevBar logs errors, data changes and crashes (with stack trace) to `%AppData%\DevBar\devbar.log`. The log is capped at 1 MB; the previous one is kept as `devbar.log.1`. Include both when reporting a problem.

## JSON endpoint format

DevBar is compatible with the same JSON format as the [original macOS DevBar](https://github.com/boxed/DevBar#usage). It requests your URL with `?username=<your Windows username>` appended, and your endpoint should return:

```json
{
    "data": {
        "category_name": [
            { "title": "Item title", "url": "https://..." }
        ]
    },
    "metadata": {
        "display": {
            "category_name": {
                "priority": 0,
                "symbol": "💥",
                "title": "💥 Category Title"
            }
        }
    }
}
```

Pills are ordered by `priority` (lowest first). The built-in category → status colors are in `CategoryStatus` in `OverlayWindow.xaml.cs`.

## Building from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer on Windows.

```
dotnet build
dotnet run
```

To build the single-file release exe:

```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```
