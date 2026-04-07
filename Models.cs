using System.Text.Json.Serialization;

namespace DevBar;

public record DevBarItem(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url);

public record CategoryDisplay(
    [property: JsonPropertyName("priority")] int Priority,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("title")] string Title);

public record DevBarMetadata(
    [property: JsonPropertyName("display")] Dictionary<string, CategoryDisplay> Display);

public record DevBarResult(
    [property: JsonPropertyName("metadata")] DevBarMetadata Metadata,
    [property: JsonPropertyName("data")] Dictionary<string, List<DevBarItem>> Data);
