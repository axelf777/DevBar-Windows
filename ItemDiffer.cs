namespace DevBar;

public static class ItemDiffer
{
    /// <summary>
    /// Returns the set of categories that gained at least one new item (by URL)
    /// since the previous result. Returns empty when there is no previous result,
    /// so a fresh start does not animate every category.
    /// </summary>
    public static IReadOnlySet<string> GetChangedCategories(DevBarResult? previous, DevBarResult current)
    {
        var changed = new HashSet<string>();
        if (previous is null) return changed;

        var previousUrls = new Dictionary<string, HashSet<string>>();
        foreach (var (category, items) in previous.Data)
            previousUrls[category] = items.Select(i => i.Url).ToHashSet();

        foreach (var (category, items) in current.Data)
        {
            var oldUrls = previousUrls.GetValueOrDefault(category);
            foreach (var item in items)
            {
                if (oldUrls is null || !oldUrls.Contains(item.Url))
                {
                    changed.Add(category);
                    break;
                }
            }
        }

        return changed;
    }
}
