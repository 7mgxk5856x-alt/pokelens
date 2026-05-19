using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokelensTools;

public class PokeAPIFetcher
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions WriteOptions =
        new() { WriteIndented = true };
    private const int ConcurrencyLimit = 8;

    // Per-num caches dedupe pokemon-species fetches across variants of the same dex number.
    private readonly ConcurrentDictionary<int, Task<JsonNode?>> _speciesCache = new();

    public PokeAPIFetcher(HttpClient http)
    {
        _http = http;
    }

    // Fetches Japanese name translations for all Pokémon, moves, abilities, and items.
    public async Task FetchTranslations(
        string cacheDir,
        string showdownPokedexPath,
        string showdownMovesPath,
        string showdownItemsPath,
        string showdownAbilitiesPath)
    {
        _speciesCache.Clear();

        Console.WriteLine("  Fetching Pokémon names from PokéAPI...");
        var pokemon = await FetchPokemonNames(showdownPokedexPath);

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
        var items = await FetchItemNames(showdownItemsPath);

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

    // Pokémon-specific fetcher that resolves forme-level Japanese names.
    // - Base entries (no forme) → species name (e.g., "ロトム").
    // - Variant entries → pokemon-form's form_names. PokéAPI sometimes returns just
    //   the form qualifier (e.g., "けんのおう" for Zacian-Crowned), so when the form
    //   name doesn't contain the species name, combine as "species (form)".
    private async Task<JsonObject> FetchPokemonNames(string showdownPokedexPath)
    {
        var showdownData = JsonNode.Parse(File.ReadAllText(showdownPokedexPath))!.AsObject();

        var targets = new List<(string Key, int Num, string Name, string? Forme)>();
        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;
            var englishName = entry["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(englishName)) continue;
            var forme = entry["forme"]?.GetValue<string>();
            targets.Add((key, num, englishName, forme));
        }

        using var gate = new SemaphoreSlim(ConcurrencyLimit);
        var resolved = new (string Key, string? JaName)[targets.Count];
        var tasks = new Task[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            var idx = i;
            var t = targets[idx];
            tasks[idx] = Task.Run(async () =>
            {
                await gate.WaitAsync();
                try
                {
                    var ja = await ResolvePokemonJapaneseName(t.Name, t.Num, t.Forme);
                    resolved[idx] = (t.Key, ja);
                    if (ja == null)
                    {
                        var slug = DerivePokemonFormSlug(t.Name);
                        Console.WriteLine(
                            $"    Warning: no Japanese name for pokemon/{t.Key} (id={t.Num}, slug={slug})");
                    }
                }
                finally { gate.Release(); }
            });
        }
        await Task.WhenAll(tasks);

        var result = new JsonObject();
        foreach (var (key, ja) in resolved)
        {
            if (ja != null) result[key] = ja;
        }
        return result;
    }

    private async Task<string?> ResolvePokemonJapaneseName(
        string showdownName, int num, string? forme)
    {
        var speciesName = await GetSpeciesJaName(num);

        if (string.IsNullOrEmpty(forme))
            return speciesName;

        var formName = await GetFormJaName(showdownName, num);
        if (string.IsNullOrEmpty(formName))
            return speciesName;

        if (!string.IsNullOrEmpty(speciesName) && formName.Contains(speciesName))
            return formName;
        if (!string.IsNullOrEmpty(speciesName))
            return $"{speciesName} ({formName})";
        return formName;
    }

    private async Task<string?> GetSpeciesJaName(int num)
    {
        var node = await GetSpeciesNode(num);
        return node == null ? null : ExtractJaName(node, "names");
    }

    private Task<JsonNode?> GetSpeciesNode(int num)
    {
        return _speciesCache.GetOrAdd(num, async n =>
        {
            var body = await FetchTextOrNull($"https://pokeapi.co/api/v2/pokemon-species/{n}/");
            return body == null ? null : JsonNode.Parse(body);
        });
    }

    // Try the Showdown-derived slug; if that misses, walk pokemon-species varieties to
    // find a matching slug (e.g., Showdown "ogerponwellspring" → "Ogerpon-Wellspring" →
    // slug "ogerpon-wellspring" → PokéAPI variety "ogerpon-wellspring-mask").
    private async Task<string?> GetFormJaName(string showdownName, int num)
    {
        var slug = DerivePokemonFormSlug(showdownName);
        var node = await FetchFormNode(slug);
        if (node != null)
        {
            var ja = ExtractJaName(node, "form_names");
            if (!string.IsNullOrEmpty(ja)) return ja;
        }

        var species = await GetSpeciesNode(num);
        if (species == null) return null;
        var matched = FindMatchingVariety(species, slug);
        if (matched == null || matched == slug) return null;

        var fallback = await FetchFormNode(matched);
        if (fallback == null) return null;
        return ExtractJaName(fallback, "form_names");
    }

    private async Task<JsonNode?> FetchFormNode(string slug)
    {
        var body = await FetchTextOrNull($"https://pokeapi.co/api/v2/pokemon-form/{slug}/");
        return body == null ? null : JsonNode.Parse(body);
    }

    public static string? FindMatchingVariety(JsonNode speciesNode, string targetSlug)
    {
        var varieties = speciesNode["varieties"]?.AsArray();
        if (varieties == null) return null;
        string? best = null;
        int bestLen = -1;
        foreach (var v in varieties)
        {
            var name = v?["pokemon"]?["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(name)) continue;
            if ((name.StartsWith(targetSlug) || targetSlug.StartsWith(name))
                && name.Length > bestLen)
            {
                best = name;
                bestLen = name.Length;
            }
        }
        return best;
    }

    // Showdown's item `name` (e.g., "Choice Scarf", "Wellspring Mask") → PokéAPI item slug.
    public static string DeriveItemSlug(string showdownName)
    {
        var lower = showdownName.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            if (c == '\'' || c == '’' || c == '.' || c == ':' || c == ',') continue;
            if (c == ' ' || c == '_') sb.Append('-');
            else if (c == 'é' || c == 'è' || c == 'ê' || c == 'ë') sb.Append('e');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    // Slug-based item name fetcher. Showdown's numeric IDs diverge from PokéAPI's,
    // so we resolve by hyphenated lowercase name instead.
    private async Task<JsonObject> FetchItemNames(string showdownItemsPath)
    {
        var showdownData = JsonNode.Parse(File.ReadAllText(showdownItemsPath))!.AsObject();

        var targets = new List<(string Key, string Name)>();
        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;
            var name = entry["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(name)) continue;
            targets.Add((key, name));
        }

        using var gate = new SemaphoreSlim(ConcurrencyLimit);
        var resolved = new (string Key, string? JaName)[targets.Count];
        var tasks = new Task[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            var idx = i;
            var t = targets[idx];
            tasks[idx] = Task.Run(async () =>
            {
                await gate.WaitAsync();
                try
                {
                    var slug = DeriveItemSlug(t.Name);
                    var ja = await FetchJapaneseName($"https://pokeapi.co/api/v2/item/{slug}/");
                    resolved[idx] = (t.Key, ja);
                    if (ja == null)
                        Console.WriteLine($"    Warning: no Japanese name for items/{t.Key} (slug={slug})");
                }
                finally { gate.Release(); }
            });
        }
        await Task.WhenAll(tasks);

        var result = new JsonObject();
        foreach (var (key, ja) in resolved)
        {
            if (ja != null) result[key] = ja;
        }
        return result;
    }

    // Showdown's `name` (e.g., "Rotom-Wash", "Mr. Mime", "Flabébé") → PokéAPI form slug.
    public static string DerivePokemonFormSlug(string showdownName)
    {
        var lower = showdownName.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower)
        {
            if (c == '\'' || c == '’' || c == '.' || c == ':' || c == ',' || c == '%') continue;
            if (c == ' ' || c == '_') sb.Append('-');
            else if (c == 'é' || c == 'è' || c == 'ê' || c == 'ë') sb.Append('e');
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private async Task<JsonObject> FetchCategory(
        string showdownCachePath,
        Func<int, string> urlBuilder,
        string categoryName)
    {
        var showdownData = JsonNode.Parse(File.ReadAllText(showdownCachePath))!.AsObject();

        var targets = new List<(string Key, int Num)>();
        foreach (var (key, val) in showdownData)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;
            targets.Add((key, num));
        }

        using var gate = new SemaphoreSlim(ConcurrencyLimit);
        var resolved = new (string Key, string? JaName)[targets.Count];
        var tasks = new Task[targets.Count];
        for (int i = 0; i < targets.Count; i++)
        {
            var idx = i;
            var t = targets[idx];
            tasks[idx] = Task.Run(async () =>
            {
                await gate.WaitAsync();
                try
                {
                    var ja = await FetchJapaneseName(urlBuilder(t.Num));
                    resolved[idx] = (t.Key, ja);
                    if (ja == null)
                        Console.WriteLine($"    Warning: no Japanese name for {categoryName}/{t.Key} (id={t.Num})");
                }
                finally { gate.Release(); }
            });
        }
        await Task.WhenAll(tasks);

        var result = new JsonObject();
        foreach (var (key, ja) in resolved)
        {
            if (ja != null) result[key] = ja;
        }
        return result;
    }

    public async Task<string?> FetchJapaneseName(string url)
    {
        var body = await FetchTextOrNull(url);
        if (body == null) return null;
        var node = JsonNode.Parse(body);
        if (node == null) return null;
        return ExtractJaName(node, "names");
    }

    private async Task<string?> FetchTextOrNull(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"    Warning: HTTP error for {url}: {ex.Message}");
            return null;
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest) return null;
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"    Warning: {response.StatusCode} for {url}");
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    private static string? ExtractJaName(JsonNode root, string arrayKey)
    {
        var arr = root[arrayKey]?.AsArray();
        if (arr == null) return null;

        string? jaHrkt = null;
        foreach (var entry in arr)
        {
            var lang = entry?["language"]?["name"]?.GetValue<string>();
            var name = entry?["name"]?.GetValue<string>();
            if (string.IsNullOrEmpty(name)) continue;
            if (lang == "ja") return name;
            if (lang == "ja-Hrkt" || lang == "ja-hrkt") jaHrkt = name;
        }
        return jaHrkt;
    }
}
