using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokelensTools;

public static class PokeAPIFetcher
{
    private static readonly HttpClient Http = new();
    private static readonly JsonSerializerOptions WriteOptions =
        new() { WriteIndented = true };

    // Fetches Japanese name translations for all Pokémon, moves, abilities, and items.
    // Sequential to avoid PokéAPI rate limiting.
    public static async Task FetchTranslations(
        string cacheDir,
        string showdownPokedexPath,
        string showdownMovesPath,
        string showdownItemsPath,
        string showdownAbilitiesPath)
    {
        Console.WriteLine("  Fetching Pokémon names from PokéAPI...");
        var pokemon = await FetchCategory(
            showdownPokedexPath,
            id => $"https://pokeapi.co/api/v2/pokemon-species/{id}/",
            "pokemon");

        Console.WriteLine("  Fetching move names from PokéAPI...");
        var moves = await FetchCategory(
            showdownMovesPath,
            id => $"https://pokeapi.co/api/v2/move/{id}/",
            "moves");

        Console.WriteLine("  Fetching ability names from PokéAPI...");
        var abilities = await FetchCategory(
            showdownAbilitiesPath,
            id => $"https://pokeapi.co/api/v2/ability/{id}/",
            "abilities");

        Console.WriteLine("  Fetching item names from PokéAPI...");
        var items = await FetchCategory(
            showdownItemsPath,
            id => $"https://pokeapi.co/api/v2/item/{id}/",
            "items");

        var translations = new JsonObject
        {
            ["pokemon"] = pokemon,
            ["moves"] = moves,
            ["abilities"] = abilities,
            ["items"] = items,
        };

        Directory.CreateDirectory(cacheDir);
        File.WriteAllText(
            Path.Combine(cacheDir, "pokeapi-translations.json"),
            translations.ToJsonString(WriteOptions));
    }

    private static async Task<JsonObject> FetchCategory(
        string showdownCachePath,
        Func<int, string> urlBuilder,
        string categoryName)
    {
        var result = new JsonObject();

        var showdownData = JsonNode.Parse(File.ReadAllText(showdownCachePath))!.AsObject();

        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;

            var jaName = await FetchJapaneseName(urlBuilder(num));
            if (jaName != null)
                result[key] = jaName;
            else
                Console.WriteLine($"    Warning: no Japanese name for {categoryName}/{key} (id={num})");
        }

        return result;
    }

    public static async Task<string?> FetchJapaneseName(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await Http.GetAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Warning: HTTP error for {url}: {ex.Message}");
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"    Warning: {response.StatusCode} for {url}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("names", out var names)) return null;

        foreach (var name in names.EnumerateArray())
        {
            if (!name.TryGetProperty("language", out var lang)) continue;
            if (!lang.TryGetProperty("name", out var langName)) continue;
            if (langName.GetString() != "ja") continue;
            if (!name.TryGetProperty("name", out var jaName)) continue;
            return jaName.GetString();
        }

        return null;
    }
}
