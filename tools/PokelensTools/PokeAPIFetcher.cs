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

    // Per-num cache that dedupes pokemon-species fetches across variants of the same dex number
    // (e.g., Rotom and Rotom-Wash both resolve via species num=479 → single HTTP call).
    //
    // Lifetime contract: FetchTranslationsAsync is expected to be called at most once per
    // PokeAPIFetcher instance (Program.cs creates a new instance per CLI invocation). The
    // Clear() at the top of FetchTranslationsAsync defends against accidental re-use, but if
    // the contract is violated while a previous call's tasks are still in flight, the cache
    // can be cleared mid-flight and partial results may be observed. If concurrent or repeated
    // translation runs ever become a requirement, drop this field and pass a local
    // ConcurrentDictionary down the call chain instead.
    private readonly ConcurrentDictionary<int, Task<JsonNode?>> _speciesCache = new();

    public PokeAPIFetcher(HttpClient http)
    {
        _http = http;
    }

    // Fetches Japanese name translations for all Pokémon, moves, abilities, and items.
    public async Task FetchTranslationsAsync(
        string cacheDir,
        string showdownPokedexPath,
        string showdownMovesPath,
        string showdownItemsPath,
        string showdownAbilitiesPath)
    {
        _speciesCache.Clear();

        Console.WriteLine("  Fetching Pokémon names from PokéAPI...");
        var pokemon = await FetchPokemonNamesAsync(showdownPokedexPath);

        Console.WriteLine("  Fetching move names from PokéAPI...");
        var moves = await FetchCategoryAsync(
            showdownMovesPath,
            id => $"https://pokeapi.co/api/v2/move/{id}/",
            "moves");

        Console.WriteLine("  Fetching ability names from PokéAPI...");
        var abilities = await FetchCategoryAsync(
            showdownAbilitiesPath,
            id => $"https://pokeapi.co/api/v2/ability/{id}/",
            "abilities");

        Console.WriteLine("  Fetching item names from PokéAPI...");
        var items = await FetchItemNamesAsync(showdownItemsPath);

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
    private async Task<JsonObject> FetchPokemonNamesAsync(string showdownPokedexPath)
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

        return await RunParallelAsync(targets, async t =>
        {
            var ja = await ResolvePokemonJapaneseNameAsync(t.Name, t.Num, t.Forme);
            if (ja == null)
            {
                var slug = DerivePokemonFormSlug(t.Name);
                Console.WriteLine(
                    $"    Warning: no Japanese name for pokemon/{t.Key} (id={t.Num}, slug={slug})");
            }
            return (t.Key, ja);
        });
    }

    private async Task<string?> ResolvePokemonJapaneseNameAsync(
        string showdownName, int num, string? forme)
    {
        var speciesName = await GetSpeciesJaNameAsync(num);

        if (string.IsNullOrEmpty(forme))
            return speciesName;

        var formName = await GetFormJaNameAsync(showdownName, num);
        if (string.IsNullOrEmpty(formName))
            return speciesName;

        if (!string.IsNullOrEmpty(speciesName) && formName.Contains(speciesName))
            return formName;
        if (!string.IsNullOrEmpty(speciesName))
            return $"{speciesName} ({formName})";
        return formName;
    }

    private async Task<string?> GetSpeciesJaNameAsync(int num)
    {
        var node = await GetSpeciesNodeAsync(num);
        return node == null ? null : ExtractJaName(node, "names");
    }

    private Task<JsonNode?> GetSpeciesNodeAsync(int num)
    {
        return _speciesCache.GetOrAdd(num, async n =>
        {
            var body = await FetchTextOrNullAsync($"https://pokeapi.co/api/v2/pokemon-species/{n}/");
            return body == null ? null : JsonNode.Parse(body);
        });
    }

    // Try the Showdown-derived slug; if that misses, walk pokemon-species varieties to
    // find a matching slug (e.g., Showdown "ogerponwellspring" → "Ogerpon-Wellspring" →
    // slug "ogerpon-wellspring" → PokéAPI variety "ogerpon-wellspring-mask").
    private async Task<string?> GetFormJaNameAsync(string showdownName, int num)
    {
        var slug = DerivePokemonFormSlug(showdownName);
        var node = await FetchFormNodeAsync(slug);
        if (node != null)
        {
            var ja = ExtractJaName(node, "form_names");
            if (!string.IsNullOrEmpty(ja)) return ja;
        }

        var species = await GetSpeciesNodeAsync(num);
        if (species == null) return null;
        var matched = FindMatchingVariety(species, slug);
        if (matched == null || matched == slug) return null;

        var fallback = await FetchFormNodeAsync(matched);
        if (fallback == null) return null;
        return ExtractJaName(fallback, "form_names");
    }

    private async Task<JsonNode?> FetchFormNodeAsync(string slug)
    {
        var body = await FetchTextOrNullAsync($"https://pokeapi.co/api/v2/pokemon-form/{slug}/");
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
    private async Task<JsonObject> FetchItemNamesAsync(string showdownItemsPath)
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

        return await RunParallelAsync(targets, async t =>
        {
            var slug = DeriveItemSlug(t.Name);
            var ja = await FetchJapaneseNameAsync($"https://pokeapi.co/api/v2/item/{slug}/");
            if (ja == null)
                Console.WriteLine($"    Warning: no Japanese name for items/{t.Key} (slug={slug})");
            return (t.Key, ja);
        });
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

    private async Task<JsonObject> FetchCategoryAsync(
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

        return await RunParallelAsync(targets, async t =>
        {
            var ja = await FetchJapaneseNameAsync(urlBuilder(t.Num));
            if (ja == null)
                Console.WriteLine($"    Warning: no Japanese name for {categoryName}/{t.Key} (id={t.Num})");
            return (t.Key, ja);
        });
    }

    // Run a resolver across targets with bounded parallelism and collect non-null results
    // into a JsonObject keyed by the resolver's Key. Centralizes the SemaphoreSlim gating
    // and result aggregation shared by the three FetchXxxAsync methods.
    private async Task<JsonObject> RunParallelAsync<T>(
        IReadOnlyList<T> targets,
        Func<T, Task<(string Key, string? JaName)>> resolver)
    {
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
                try { resolved[idx] = await resolver(t); }
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

    public async Task<string?> FetchJapaneseNameAsync(string url)
    {
        var body = await FetchTextOrNullAsync(url);
        if (body == null) return null;
        var node = JsonNode.Parse(body);
        if (node == null) return null;
        return ExtractJaName(node, "names");
    }

    private async Task<string?> FetchTextOrNullAsync(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (TaskCanceledException)
        {
            // HttpClient surfaces request timeouts as TaskCanceledException.
            // No CancellationToken is passed, so external cancellation is not a possibility here.
            Console.WriteLine($"    Warning: HTTP timeout for {url}");
            return null;
        }
        catch (HttpRequestException ex)
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

    public static string? ExtractJaName(JsonNode root, string arrayKey)
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
