using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace PokelensTools;

/// <summary>
/// Pokémon Showdown のデータ JS（pokedex / moves / items / abilities）を取得し、
/// 現代対戦で必要なフィールドだけを抽出して cache/ に保存する。
/// </summary>
internal class ShowdownFetcher
{
    private readonly HttpClient _http;

    internal ShowdownFetcher(HttpClient http)
    {
        _http = http;
    }

    /// <summary>4 種のデータ（pokedex / moves / items / abilities）を並行取得して cache/ に書き出す。</summary>
    internal async Task FetchAllAsync(string cacheDir)
    {
        Directory.CreateDirectory(cacheDir);
        await Task.WhenAll(
            FetchPokedexAsync(cacheDir),
            FetchMovesAsync(cacheDir),
            FetchItemsAsync(cacheDir),
            FetchAbilitiesAsync(cacheDir)
        );
    }

    internal async Task FetchPokedexAsync(string cacheDir)
    {
        string js = await FetchTextAsync("https://play.pokemonshowdown.com/data/pokedex.js");
        JsonObject root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildPokedexEntry(entry) is { } pokedexEntry)
            {
                filtered[key] = pokedexEntry;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(cacheDir, "showdown-pokedex.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    /// <summary>
    /// Showdown のポケモンエントリを成果物用に整形する。num が 0 以下、または baseStats が無い場合は対象外として null を返す。
    /// </summary>
    internal static JsonObject? BuildPokedexEntry(JsonObject entry)
    {
        int num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0)
        {
            return null;
        }

        JsonObject? baseStats = entry["baseStats"]?.AsObject();
        if (baseStats == null)
        {
            return null;
        }

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

        string? forme = entry["forme"]?.GetValue<string>();
        if (!string.IsNullOrEmpty(forme))
        {
            pokedexEntry["forme"] = forme;
        }

        return pokedexEntry;
    }

    internal async Task FetchMovesAsync(string cacheDir)
    {
        string js = await FetchTextAsync("https://play.pokemonshowdown.com/data/moves.js");
        JsonObject root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildMoveEntry(entry) is { } moveEntry)
            {
                filtered[key] = moveEntry;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(cacheDir, "showdown-moves.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    /// <summary>
    /// Showdown の技エントリを成果物用に整形する。num が 0 以下、または Z ワザ・(キョダイ)ダイマックスワザは対象外として null を返す。
    /// </summary>
    internal static JsonObject? BuildMoveEntry(JsonObject entry)
    {
        int num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0)
        {
            return null;
        }
        // Zワザ・ダイマックスワザ・キョダイマックスワザは現代対戦 (Gen 9 標準) で
        // 使用不可のためここで除外する。functional-design.md 「除外フィルタ」参照。
        if (entry["isZ"] != null || entry["isMax"] != null)
        {
            return null;
        }

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

        if (entry["multihit"] is { } multihit)
        {
            moveEntry["multihit"] = multihit.DeepClone();
        }

        if (entry["recoil"] != null)
        {
            moveEntry["recoil"] = JsonValue.Create(true);
        }

        if (entry["secondary"] is JsonObject secondary)
        {
            moveEntry["secondary"] = secondary.DeepClone();
        }

        if (entry["secondaries"] is JsonArray secondaries)
        {
            moveEntry["secondaries"] = secondaries.DeepClone();
        }

        return moveEntry;
    }

    internal async Task FetchItemsAsync(string cacheDir)
    {
        string js = await FetchTextAsync("https://play.pokemonshowdown.com/data/items.js");
        JsonObject root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildItemEntry(entry) is { } itemEntry)
            {
                filtered[key] = itemEntry;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(cacheDir, "showdown-items.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    /// <summary>
    /// Showdown のアイテムエントリを成果物用に整形する。num が 0 以下、または非標準（Past / Future / CAP）は対象外として null を返す。
    /// </summary>
    internal static JsonObject? BuildItemEntry(JsonObject entry)
    {
        int num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0)
        {
            return null;
        }
        // 過去世代限定 (Past) / 次世代仮置き (Future) / CAP 由来のアイテムは現代対戦の対象外。
        // functional-design.md 「除外フィルタ」参照。
        if (entry["isNonstandard"] != null)
        {
            return null;
        }

        return new JsonObject
        {
            ["num"] = num,
            ["name"] = entry["name"]?.GetValue<string>(),
        };
    }

    internal async Task FetchAbilitiesAsync(string cacheDir)
    {
        string js = await FetchTextAsync("https://play.pokemonshowdown.com/data/abilities.js");
        JsonObject root = JsonNode.Parse(JsToJson(js))!.AsObject();

        var filtered = new JsonObject();
        foreach (var (key, val) in root)
        {
            if (val is JsonObject entry && BuildAbilityEntry(entry) is { } abilityEntry)
            {
                filtered[key] = abilityEntry;
            }
        }

        await File.WriteAllTextAsync(
            Path.Combine(cacheDir, "showdown-abilities.json"),
            JsonHelpers.ToIndentedJson(filtered));
    }

    /// <summary>
    /// Showdown の特性エントリを成果物用に整形する。num が 0 以下、または非標準（Past / Future / CAP）は対象外として null を返す。
    /// </summary>
    internal static JsonObject? BuildAbilityEntry(JsonObject entry)
    {
        int num = entry["num"]?.GetValue<int>() ?? 0;
        if (num <= 0)
        {
            return null;
        }
        // 過去世代限定 (Past) / 次世代仮置き (Future) / CAP 由来の特性は現代対戦の対象外。
        // functional-design.md 「除外フィルタ」参照。
        if (entry["isNonstandard"] != null)
        {
            return null;
        }

        return new JsonObject { ["num"] = num };
    }

    private async Task<string> FetchTextAsync(string url)
    {
        HttpResponseMessage response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to fetch {url}: {(int)response.StatusCode} {response.StatusCode}",
                inner: null,
                statusCode: response.StatusCode);
        }

        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Showdown の JS オブジェクトリテラルを JSON に変換する。クオートなしのプロパティキーと
    /// 末尾カンマ（trailing comma）に対応する。
    /// </summary>
    internal static string JsToJson(string js)
    {
        int start = js.IndexOf('{');
        int end = js.LastIndexOf('}');
        if (start < 0 || end < 0)
        {
            throw new FormatException("Invalid Showdown JS: no top-level object found");
        }

        string obj = js[start..(end + 1)];
        obj = QuoteUnquotedKeys(obj);
        // } または ] の直前の末尾カンマ（trailing comma）を除去する
        obj = Regex.Replace(obj, @",(\s*[}\]])", "$1");
        return obj;
    }

    // クオートなしの JS オブジェクトキーをクオートで囲むステートマシン。
    // 配列の値を誤ってクオートしないよう、オブジェクト／配列のコンテキストを追跡する。
    private static string QuoteUnquotedKeys(string input)
    {
        var sb = new StringBuilder(input.Length + 4096);
        var context = new Stack<char>(); // '{' = オブジェクト, '[' = 配列
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
                    else if (s == '"')
                    {
                        break;
                    }
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
                if (context.Count > 0 && context.Peek() == '{')
                {
                    context.Pop();
                }

                sb.Append(c);
                i++;
            }
            else if (c == ']')
            {
                if (context.Count > 0 && context.Peek() == '[')
                {
                    context.Pop();
                }

                sb.Append(c);
                i++;
            }
            else if (c == ',')
            {
                sb.Append(c);
                i++;
                if (context.Count > 0 && context.Peek() == '{')
                {
                    AppendWhitespaceAndMaybeQuoteKey(input, sb, ref i);
                }
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

        if (i >= input.Length || input[i] == '"')
        {
            return;
        }

        if (!char.IsLetter(input[i]) && !char.IsDigit(input[i]) && input[i] != '_')
        {
            return;
        }

        sb.Append('"');
        while (i < input.Length && (char.IsLetterOrDigit(input[i]) || input[i] == '_'))
        {
            sb.Append(input[i]);
            i++;
        }
        sb.Append('"');
    }
}
