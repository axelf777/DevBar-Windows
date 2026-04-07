namespace DevBar;

public static class ItemDiffer
{
    public static List<(string Category, string Symbol, DevBarItem Item)> GetNewItems(
        DevBarResult? previous, DevBarResult current)
    {
        var newItems = new List<(string, string, DevBarItem)>();
        if (previous is null) return newItems;

        var previousUrls = new Dictionary<string, HashSet<string>>();
        foreach (var (category, items) in previous.Data)
            previousUrls[category] = items.Select(i => i.Url).ToHashSet();

        foreach (var (category, items) in current.Data)
        {
            var oldUrls = previousUrls.GetValueOrDefault(category);
            var symbol = current.Metadata.Display.TryGetValue(category, out var display)
                ? display.Symbol : "";

            foreach (var item in items)
            {
                if (oldUrls is null || !oldUrls.Contains(item.Url))
                    newItems.Add((category, symbol, item));
            }
        }

        return newItems;
    }
}
