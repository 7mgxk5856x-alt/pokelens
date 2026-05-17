using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PokelensTools;

public static class ShowdownFetcher
{
    private static readonly HttpClient Http = new();

    public static async Task FetchAll(string cacheDir)
    {
        Directory.CreateDirectory(cacheDir);
        await Task.WhenAll(
            FetchPokedex(cacheDir),
            FetchMoves(cacheDir),
            FetchItems(cacheDir),
            FetchAbilities(cacheDir)
        );
    }

    public static async Task FetchPokedex(string cacheDir)
    {
        var js = await FetchText("https://play.pokemonshowdown.com/data/pokedex.js");
        var json = JsToJson(js);
        var root = JsonNode.Parse(json)!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;

            var baseStats = entry["baseStats"]?.AsObject();
            if (baseStats == null) continue;

            var filteredEntry = new JsonObject
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
            if (!string.IsNullOrEmpty(forme)) filteredEntry["forme"] = forme;

            filtered[key] = filteredEntry;
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-pokedex.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static async Task FetchMoves(string cacheDir)
    {
        var js = await FetchText("https://play.pokemonshowdown.com/data/moves.js");
        var json = JsToJson(js);
        var root = JsonNode.Parse(json)!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;

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

            var multihit = entry["multihit"];
            if (multihit != null) moveEntry["multihit"] = multihit.DeepClone();

            var recoil = entry["recoil"];
            if (recoil != null) moveEntry["recoil"] = JsonValue.Create(true);

            var secondary = entry["secondary"];
            if (secondary is JsonObject) moveEntry["secondary"] = secondary.DeepClone();

            var secondaries = entry["secondaries"];
            if (secondaries is JsonArray) moveEntry["secondaries"] = secondaries.DeepClone();

            var isZ = entry["isZ"];
            if (isZ != null) moveEntry["isZ"] = JsonValue.Create(true);

            var isMax = entry["isMax"];
            if (isMax != null) moveEntry["isMax"] = JsonValue.Create(true);

            filtered[key] = moveEntry;
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-moves.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static async Task FetchItems(string cacheDir)
    {
        var js = await FetchText("https://play.pokemonshowdown.com/data/items.js");
        var json = JsToJson(js);
        var root = JsonNode.Parse(json)!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;
            if (entry["isNonstandard"] != null) continue;
            filtered[key] = new JsonObject
            {
                ["num"] = num,
                ["name"] = entry["name"]?.GetValue<string>(),
            };
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-items.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    public static async Task FetchAbilities(string cacheDir)
    {
        var js = await FetchText("https://play.pokemonshowdown.com/data/abilities.js");
        var json = JsToJson(js);
        var root = JsonNode.Parse(json)!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is not JsonObject entry) continue;
            var num = entry["num"]?.GetValue<int>() ?? 0;
            if (num <= 0) continue;
            if (entry["isNonstandard"] != null) continue;
            filtered[key] = new JsonObject { ["num"] = num };
        }

        File.WriteAllText(
            Path.Combine(cacheDir, "showdown-abilities.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    private static async Task<string> FetchText(string url)
    {
        var response = await Http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to fetch {url}: {response.StatusCode}");
        return await response.Content.ReadAsStringAsync();
    }

    // Converts Showdown JS object literal to JSON.
    // Handles unquoted property keys and trailing commas.
    public static string JsToJson(string js)
    {
        int start = js.IndexOf('{');
        int end = js.LastIndexOf('}');
        if (start < 0 || end < 0)
            throw new Exception("Invalid Showdown JS: no top-level object found");

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
