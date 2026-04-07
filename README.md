# DevBar for Windows

Windows port of [boxed/DevBar](https://github.com/boxed/DevBar) — a system tray app that polls a JSON endpoint and displays developer workflow status via a color-coded icon, toast notifications, and a rich popup.

## Features

- **Color-coded tray icon** — green (all clear), amber (items exist), red (high priority), gray (server down)
- **Click to expand** — left-click shows a dark-themed popup with categorized items and emoji
- **Toast notifications** — fires when new items appear, click to open in browser
- **Hover tooltip** — shows category breakdown at a glance
- **Battery-aware polling** — throttles to 30s intervals on battery power
- **503 handling** — keeps showing old data while server warms up

## Setup

1. Download `DevBar.exe` from [Releases](https://github.com/axelf777/DevBar-Windows/releases)
2. Run it — a Preferences window will open on first launch
3. Enter your DevBar JSON endpoint URL and click Save

### Pin the icon to your taskbar

By default Windows hides new tray icons in the overflow area. To keep DevBar always visible:

1. Right-click the taskbar
2. Select **Taskbar settings**
3. Scroll down to **Other system tray icons**
4. Toggle **DevBar** to **On**

## JSON endpoint format

DevBar is compatible with the same JSON format as the [original macOS DevBar](https://github.com/boxed/DevBar#usage). Your endpoint should return:

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

Categories with `priority < 10` are treated as high priority (red icon).

## Building from source

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.

```
dotnet build
dotnet run
```
