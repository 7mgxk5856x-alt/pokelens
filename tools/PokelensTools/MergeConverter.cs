using System.Text.Json.Nodes;

namespace PokelensTools;

/// <summary>
/// Showdown のキャッシュと PokéAPI 翻訳・各種パッチをマージし、フロントエンド用の
/// 成果物 JSON（pokedex / moves / items / abilities）を data/ に生成する。
/// </summary>
internal static class MergeConverter
{
    // Showdown の flag キー → JSON タグ名の対応表（例外を先に引き、無ければ汎用ルール）
    private static readonly Dictionary<string, string> FlagExceptions = new()
    {
        ["slicing"] = "isSlice",
    };

    /// <summary>Showdown の flag キーを成果物のタグ名（例: "isSlice"）に変換する。</summary>
    public static string FlagToTag(string flag)
    {
        if (FlagExceptions.TryGetValue(flag, out string? exception))
        {
            return exception;
        }

        return "is" + char.ToUpperInvariant(flag[0]) + flag[1..];
    }

    /// <summary>
    /// 全入力（Showdown キャッシュ・翻訳・パッチ・修正子）を読み込んでマージし、
    /// data/ 配下に pokedex / moves / items / abilities の各 JSON を書き出す。
    /// </summary>
    public static void Convert(
        string showdownPokedexPath,
        string showdownMovesPath,
        string showdownItemsPath,
        string showdownAbilitiesPath,
        string translationsPath,
        string movesPowerPatchPath,
        string itemsModifiersPath,
        string abilitiesModifiersPath,
        string pokemonNamePatchPath,
        string itemNamePatchPath,
        string dataDir)
    {
        Directory.CreateDirectory(dataDir);

        JsonObject pokedex = JsonNode.Parse(File.ReadAllText(showdownPokedexPath))!.AsObject();
        JsonObject moves = JsonNode.Parse(File.ReadAllText(showdownMovesPath))!.AsObject();
        JsonObject items = JsonNode.Parse(File.ReadAllText(showdownItemsPath))!.AsObject();
        JsonObject abilities = JsonNode.Parse(File.ReadAllText(showdownAbilitiesPath))!.AsObject();
        JsonObject translations = JsonNode.Parse(File.ReadAllText(translationsPath))!.AsObject();
        JsonObject movesPowerPatch = File.Exists(movesPowerPatchPath)
            ? JsonNode.Parse(File.ReadAllText(movesPowerPatchPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        JsonObject itemsModifiers = File.Exists(itemsModifiersPath)
            ? JsonNode.Parse(File.ReadAllText(itemsModifiersPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        JsonObject abilitiesModifiers = File.Exists(abilitiesModifiersPath)
            ? JsonNode.Parse(File.ReadAllText(abilitiesModifiersPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        JsonObject pokemonNamePatch = File.Exists(pokemonNamePatchPath)
            ? JsonNode.Parse(File.ReadAllText(pokemonNamePatchPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        JsonObject itemNamePatch = File.Exists(itemNamePatchPath)
            ? JsonNode.Parse(File.ReadAllText(itemNamePatchPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();

        JsonObject pokemonNames = translations["pokemon"]?.AsObject() ?? new JsonObject();
        JsonObject moveNames = translations["moves"]?.AsObject() ?? new JsonObject();
        JsonObject abilityNames = translations["abilities"]?.AsObject() ?? new JsonObject();
        JsonObject itemNames = translations["items"]?.AsObject() ?? new JsonObject();

        File.WriteAllText(
            Path.Combine(dataDir, "pokedex.json"),
            JsonHelpers.ToIndentedJson(ConvertPokedex(pokedex, pokemonNames, abilityNames, pokemonNamePatch)));

        File.WriteAllText(
            Path.Combine(dataDir, "moves.json"),
            JsonHelpers.ToIndentedJson(ConvertMoves(moves, moveNames, movesPowerPatch)));

        File.WriteAllText(
            Path.Combine(dataDir, "items.json"),
            JsonHelpers.ToIndentedJson(ConvertItems(itemsModifiers, itemNames, itemNamePatch)));

        File.WriteAllText(
            Path.Combine(dataDir, "abilities.json"),
            JsonHelpers.ToIndentedJson(ConvertAbilities(abilitiesModifiers, abilityNames)));
    }

    /// <summary>
    /// Showdown ポケデックスに日本語名（翻訳＋ name-patch 上書き）と日本語特性名を当て、成果物形式に変換する。
    /// </summary>
    public static JsonObject ConvertPokedex(
        JsonObject showdownPokedex,
        JsonObject pokemonNames,
        JsonObject abilityNames,
        JsonObject pokemonNamePatch)
    {
        var result = new JsonObject();

        foreach (var (key, val) in showdownPokedex)
        {
            if (val is not JsonObject entry)
            {
                continue;
            }

            if (!pokemonNames.TryGetPropertyValue(key, out JsonNode? nameNode))
            {
                continue;
            }

            string? jaName = nameNode?.GetValue<string>();
            if (string.IsNullOrEmpty(jaName))
            {
                continue;
            }

            if (pokemonNamePatch.TryGetPropertyValue(key, out JsonNode? patchNode)
                && patchNode is JsonValue patchVal
                && patchVal.TryGetValue<string>(out string? patchName)
                && !string.IsNullOrEmpty(patchName))
            {
                jaName = patchName;
            }

            JsonObject? abilitiesSlots = entry["abilities"]?.AsObject();
            var abilitiesList = new JsonArray();
            foreach (var slot in new[] { "0", "1", "H" })
            {
                if (abilitiesSlots?[slot] is JsonNode slotVal)
                {
                    string? engKey = slotVal.GetValue<string>()?.ToLowerInvariant()
                        .Replace(" ", "").Replace("-", "");
                    if (engKey != null && abilityNames.TryGetPropertyValue(engKey, out JsonNode? jaAbility))
                    {
                        abilitiesList.Add(jaAbility?.GetValue<string>());
                    }
                    else
                    {
                        abilitiesList.Add(slotVal.GetValue<string>());
                    }
                }
            }

            result[key] = new JsonObject
            {
                ["num"] = entry["num"]?.GetValue<int>(),
                ["name"] = jaName,
                ["types"] = entry["types"]?.DeepClone(),
                ["baseStats"] = entry["baseStats"]?.DeepClone(),
                ["abilities"] = abilitiesList,
            };
        }

        return result;
    }

    /// <summary>
    /// Showdown の技を成果物形式に変換する。威力（連続技・威力不定技のパッチ込み）・命中・タグを計算し、
    /// 日本語名をキーにする。
    /// </summary>
    public static JsonObject ConvertMoves(
        JsonObject showdownMoves,
        JsonObject moveNames,
        JsonObject movesPowerPatch)
    {
        var result = new JsonObject();

        foreach (var (key, val) in showdownMoves)
        {
            if (val is not JsonObject entry)
            {
                continue;
            }

            if (!moveNames.TryGetPropertyValue(key, out JsonNode? nameNode))
            {
                continue;
            }

            string? jaName = nameNode?.GetValue<string>();
            if (string.IsNullOrEmpty(jaName))
            {
                continue;
            }

            int basePower = entry["basePower"]?.GetValue<int>() ?? 0;
            JsonNode? accuracyNode = entry["accuracy"];
            JsonObject? flags = entry["flags"]?.AsObject();
            JsonNode? multihit = entry["multihit"];
            bool hasRecoil = entry["recoil"]?.GetValue<bool>() == true;

            // 威力を計算する
            int? power = basePower == 0 ? null : basePower;
            if (power != null && multihit != null)
            {
                int maxHits = multihit is JsonArray arr && arr.Count >= 2
                    ? arr[1]?.GetValue<int>() ?? 1
                    : multihit.GetValue<int>();
                power = basePower * maxHits;
            }

            // 威力が null の技には moves-power-patch を適用する
            if (power == null && movesPowerPatch.TryGetPropertyValue(key, out JsonNode? patchEntry))
            {
                int? patchPower = patchEntry?["power"]?.GetValue<int>();
                if (patchPower != null)
                {
                    power = patchPower;
                }
            }

            // 命中を計算する（true → null）
            int? accuracy = null;
            if (accuracyNode is JsonValue accVal)
            {
                if (accVal.TryGetValue<int>(out int accInt))
                {
                    accuracy = accInt;
                }
                // accuracy: true → null（必中）
            }

            // flags + recoil + secondary からタグを計算する
            var tags = new List<string>();
            if (flags != null)
            {
                foreach (var (flag, _) in flags)
                {
                    tags.Add(FlagToTag(flag));
                }
            }
            if (hasRecoil)
            {
                tags.Add("isRecoil");
            }

            // Showdownの secondary（オブジェクト）/ secondaries（配列）が存在すれば追加効果あり
            JsonNode? secondary = entry["secondary"];
            JsonNode? secondaries = entry["secondaries"];
            bool hasSecondary = secondary is JsonObject
                || (secondaries is JsonArray sArr && sArr.Count > 0);
            if (hasSecondary)
            {
                tags.Add("hasSecondary");
            }

            var moveEntry = new JsonObject
            {
                ["type"] = entry["type"]?.GetValue<string>(),
                ["category"] = entry["category"]?.GetValue<string>(),
                ["power"] = power,
                ["accuracy"] = accuracy,
            };

            if (tags.Count > 0)
            {
                var tagsArray = new JsonArray();
                foreach (var tag in tags)
                {
                    tagsArray.Add(tag);
                }

                moveEntry["tags"] = tagsArray;
            }

            result[jaName] = moveEntry;
        }

        return result;
    }

    /// <summary>
    /// アイテム修正子に日本語名（item-name-patch 優先、無ければ翻訳）を当てて成果物形式に変換する。
    /// </summary>
    public static JsonObject ConvertItems(
        JsonObject itemsModifiers,
        JsonObject itemNames,
        JsonObject itemNamePatch)
    {
        var result = new JsonObject();

        foreach (var (showdownKey, modifierEntry) in itemsModifiers)
        {
            string? jaName = null;
            if (itemNamePatch.TryGetPropertyValue(showdownKey, out JsonNode? patchNode)
                && patchNode is JsonValue patchVal
                && patchVal.TryGetValue<string>(out string? patchName)
                && !string.IsNullOrEmpty(patchName))
            {
                jaName = patchName;
            }
            else if (itemNames.TryGetPropertyValue(showdownKey, out JsonNode? nameNode))
            {
                jaName = nameNode?.GetValue<string>();
            }
            if (string.IsNullOrEmpty(jaName))
            {
                continue;
            }

            result[jaName] = modifierEntry?.DeepClone();
        }

        return result;
    }

    /// <summary>
    /// 特性修正子に日本語名（翻訳）を当てて成果物形式に変換する。
    /// </summary>
    public static JsonObject ConvertAbilities(
        JsonObject abilitiesModifiers,
        JsonObject abilityNames)
    {
        var result = new JsonObject();

        foreach (var (showdownKey, modifierEntry) in abilitiesModifiers)
        {
            if (!abilityNames.TryGetPropertyValue(showdownKey, out JsonNode? nameNode))
            {
                continue;
            }

            string? jaName = nameNode?.GetValue<string>();
            if (string.IsNullOrEmpty(jaName))
            {
                continue;
            }

            result[jaName] = modifierEntry?.DeepClone();
        }

        return result;
    }
}
