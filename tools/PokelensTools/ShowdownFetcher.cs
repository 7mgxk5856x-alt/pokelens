using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PokelensTools;

public class ShowdownFetcher
{
    private readonly HttpClient _http;

    public ShowdownFetcher(HttpClient http)
    {
        _http = http;
    }

    public async Task FetchAllAsync(string cacheDir)
    {
        Directory.CreateDirectory(cacheDir);
        await Task.WhenAll(
            FetchPokedexAsync(cacheDir),
            FetchMovesAsync(cacheDir),
            FetchItemsAsync(cacheDir),
            FetchAbilitiesAsync(cacheDir)
        );
    }

    public async Task FetchPokedexAsync(string cacheDir)
    {
        var js = await FetchTextAsync("https://play.pokemonshowdown.com/data/pokedex.js");
        var root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildPokedexEntry(entry) is { } pokedexEntry)
                filtered[key] = pokedexEntry;
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-pokedex.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static JsonObject? BuildPokedexEntry(JsonObject entry)
    {
        var num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0) return null;

        var baseStats = entry["baseStats"]?.AsObject();
        if (baseStats == null) return null;

        var pokedexEntry = new JsonObject
        {
            ["num"] = num,
            ["name"] = entry["name"]?.GetValue<string>(),
            ["types"] = entry["types"]?.DeepClone(),
            ["baseStats"] = new JsonObject
            {
                ["hp"] = baseStats["hp"]?.GetValue<int>(),
                ["atk"] = baseStats["atk"]?.GetValue<int>(),
                ["def"] = baseStats["def"]?.GetValue<int>(),
                ["spa"] = baseStats["spa"]?.GetValue<int>(),
                ["spd"] = baseStats["spd"]?.GetValue<int>(),
                ["spe"] = baseStats["spe"]?.GetValue<int>(),
            },
            ["abilities"] = entry["abilities"]?.DeepClone(),
        };

        var forme = entry["forme"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(forme)) pokedexEntry["forme"] = forme;

        return pokedexEntry;
    }

    public async Task FetchMovesAsync(string cacheDir)
    {
        var js = await FetchTextAsync("https://play.pokemonshowdown.com/data/moves.js");
        var root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildMoveEntry(entry) is { } moveEntry)
                filtered[key] = moveEntry;
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-moves.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static JsonObject? BuildMoveEntry(JsonObject entry)
    {
        var num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0) return null;
        // Zワザ・ダイマックスワザ・キョダイマックスワザは現代対戦 (Gen 9 標準) で
        // 使用不可のためここで除外する。functional-design.md 「除外フィルタ」参照。
        if (entry["isZ"] != null || entry["isMax"] != null) return null;

        var moveEntry = new JsonObject
        {
            ["num"] = num,
            ["name"] = entry["name"]?.GetValue<string>(),
            ["type"] = entry["type"]?.GetValue<string>(),
            ["category"] = entry["category"]?.GetValue<string>(),
            ["basePower"] = entry["basePower"]?.GetValue<int>() ?? 0,
            ["accuracy"] = entry["accuracy"]?.DeepClone(),
            ["flags"] = entry["flags"]?.DeepClone() ?? new JsonObject(),
        };

        if (entry["multihit"] is { } multihit) moveEntry["multihit"] = multihit.DeepClone();
        if (entry["recoil"] != null) moveEntry["recoil"] = JsonValue.Create(true);
        if (entry["secondary"] is JsonObject secondary) moveEntry["secondary"] = secondary.DeepClone();
        if (entry["secondaries"] is JsonArray secondaries) moveEntry["secondaries"] = secondaries.DeepClone();

        return moveEntry;
    }

    public async Task FetchItemsAsync(string cacheDir)
    {
        var js = await FetchTextAsync("https://play.pokemonshowdown.com/data/items.js");
        var root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildItemEntry(entry) is { } itemEntry)
                filtered[key] = itemEntry;
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-items.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static JsonObject? BuildItemEntry(JsonObject entry)
    {
        var num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0) return null;
        // 過去世代限定 (Past) / 次世代仮置き (Future) / CAP 由来のアイテムは現代対戦の対象外。
        // functional-design.md 「除外フィルタ」参照。
        if (entry["isNonstandard"] != null) return null;

        return new JsonObject
        {
            ["num"] = num,
            ["name"] = entry["name"]?.GetValue<string>(),
        };
    }

    public async Task FetchAbilitiesAsync(string cacheDir)
    {
        var js = await FetchTextAsync("https://play.pokemonshowdown.com/data/abilities.js");
        var root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildAbilityEntry(entry) is { } abilityEntry)
                filtered[key] = abilityEntry;
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-abilities.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static JsonObject? BuildAbilityEntry(JsonObject entry)
    {
        var num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0) return null;
        // 過去世代限定 (Past) / 次世代仮置き (Future) / CAP 由来の特性は現代対戦の対象外。
        // functional-design.md 「除外フィルタ」参照。
        if (entry["isNonstandard"] != null) return null;

        return new JsonObject { ["num"] = num };
    }

    private async Task<string> FetchTextAsync(string url)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Failed to fetch {url}: {(int)response.StatusCode} {response.StatusCode}",
                inner: null,
                statusCode: response.StatusCode);
        return await response.Content.ReadAsStringAsync();
    }

    // Converts Showdown JS object literal to JSON.
    // Handles unquoted property keys and trailing commas.
    public static string JsToJson(string js)
    {
        int start = js.IndexOf('{');
        int end = js.LastIndexOf('}');
        if (start < 0 || end < 0)
            throw new FormatException("Invalid Showdown JS: no top-level object found");

        string obj = js[start..(end + 1)];
        obj = QuoteUnquotedKeys(obj);
        // Remove trailing commas before } or ]
        obj = Regex.Replace(obj, @",(\s*[}\]])", "$1");
        return obj;
    }

    // State-machine that quotes unquoted JS object keys.
    // Tracks object vs array context to avoid quoting array values.
    private static string QuoteUnquotedKeys(string input)
    {
        var sb = new StringBuilder(input.Length + 4096);
        var context = new Stack<char>(); // '{' = object, '[' = array
        int i = 0;

        while (i < input.Length)
        {
            char c = input[i];

            if (c == '"')
            {
                sb.Append(c);
                i++;
                while (i < input.Length)
                {
                    char s = input[i];
                    sb.Append(s);
                    i++;
                    if (s == '\\') { if (i < input.Length) { sb.Append(input[i]); i++; } }
                    else if (s == '"') break;
                }
            }
            else if (c == '{')
            {
                context.Push('{');
                sb.Append(c);
                i++;
                AppendWhitespaceAndMaybeQuoteKey(input, sb, ref i);
            }
            else if (c == '[')
            {
                context.Push('[');
                sb.Append(c);
                i++;
            }
            else if (c == '}')
            {
                if (context.Count > 0 && context.Peek() == '{') context.Pop();
                sb.Append(c);
                i++;
            }
            else if (c == ']')
            {
                if (context.Count > 0 && context.Peek() == '[') context.Pop();
                sb.Append(c);
                i++;
            }
            else if (c == ',')
            {
                sb.Append(c);
                i++;
                if (context.Count > 0 && context.Peek() == '{')
                    AppendWhitespaceAndMaybeQuoteKey(input, sb, ref i);
            }
            else
            {
                sb.Append(c);
                i++;
            }
        }

        return sb.ToString();
    }

    private static void AppendWhitespaceAndMaybeQuoteKey(
        string input, StringBuilder sb, ref int i)
    {
        while (i < input.Length && char.IsWhiteSpace(input[i]))
        {
            sb.Append(input[i]);
            i++;
        }

        if (i >= input.Length || input[i] == '"') return;
        if (!char.IsLetter(input[i]) && !char.IsDigit(input[i]) && input[i] != '_') return;

        sb.Append('"');
        while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
        {
            sb.Append(input[i]);
            i++;
        }
        sb.Append('"');
    }
}
