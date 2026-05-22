using System.Text.Json.Nodes;

namespace PokelensTools;

/// <summary>Showdown のキャッシュと PokéAPI 翻訳・各種パッチをマージし、フロントエンド用の成果物 JSON を生成する。</summary>
/// <remarks>pokedex / moves / items / abilities の 4 ファイルを data/ に書き出す、パイプライン Step4 の中核。</remarks>
internal static class MergeConverter
{
    // Showdown の flag キー → JSON タグ名の対応表（例外を先に引き、無ければ汎用ルール）
    private static readonly Dictionary<string, string> FlagExceptions = new()
    {
        ["slicing"] = "isSlice",
    };

    /// <summary>Showdown の flag キーを成果物のタグ名（例: "isSlice"）に変換する。</summary>
    /// <remarks>例外的な対応を持つ flag は対応表を優先し、無ければ "is" + 先頭大文字化の汎用ルールを適用する。</remarks>
    /// <param name="flag">Showdown の flag キー。</param>
    /// <returns>成果物用のタグ名。</returns>
    internal static string FlagToTag(string flag)
    {
        if (FlagExceptions.TryGetValue(flag, out string? exception))
        {
            return exception;
        }

        return "is" + char.ToUpperInvariant(flag[0]) + flag[1..];
    }

    /// <summary>全入力（Showdown キャッシュ・翻訳・パッチ・修正子）を読み込んでマージし、成果物 JSON を出力する。</summary>
    /// <remarks>data/ 配下に pokedex / moves / items / abilities の各 JSON を書き出す。出力先ディレクトリは無ければ作成する。</remarks>
    /// <param name="showdownPokedexPath">Showdown ポケデックスキャッシュのパス。</param>
    /// <param name="showdownMovesPath">Showdown 技キャッシュのパス。</param>
    /// <param name="showdownItemsPath">Showdown アイテムキャッシュのパス。</param>
    /// <param name="showdownAbilitiesPath">Showdown 特性キャッシュのパス。</param>
    /// <param name="translationsPath">PokéAPI 由来の翻訳辞書のパス。</param>
    /// <param name="movesPowerPatchPath">威力不定技を補完する moves-power-patch のパス。</param>
    /// <param name="itemsModifiersPath">アイテム修正子定義のパス。</param>
    /// <param name="abilitiesModifiersPath">特性修正子定義のパス。</param>
    /// <param name="pokemonNamePatchPath">ポケモン日本語名の上書きパッチのパス。</param>
    /// <param name="itemNamePatchPath">アイテム日本語名の上書きパッチのパス。</param>
    /// <param name="dataDir">成果物 JSON の出力先ディレクトリ。</param>
    internal static void Convert(
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

    /// <summary>Showdown ポケデックスに日本語名（翻訳＋ name-patch 上書き）と日本語特性名を当て、成果物形式に変換する。</summary>
    /// <remarks>翻訳が無いエントリは出力から除外する。name-patch は翻訳由来の名前を上書きする。</remarks>
    /// <param name="showdownPokedex">Showdown のポケデックス。</param>
    /// <param name="pokemonNames">ポケモンの日本語名辞書（翻訳）。</param>
    /// <param name="abilityNames">特性の日本語名辞書（翻訳）。</param>
    /// <param name="pokemonNamePatch">ポケモン日本語名の上書きパッチ。</param>
    /// <returns>日本語名・特性を当てた成果物形式のポケデックス。</returns>
    internal static JsonObject ConvertPokedex(
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

    /// <summary>Showdown の技を成果物形式に変換する。</summary>
    /// <remarks>威力（連続技は最大ヒット数で乗算、威力不定技は moves-power-patch で補完）・命中・タグを計算し、日本語名をキーにする。翻訳が無い技は除外する。</remarks>
    /// <param name="showdownMoves">Showdown の技データ。</param>
    /// <param name="moveNames">技の日本語名辞書（翻訳）。</param>
    /// <param name="movesPowerPatch">威力不定技を補完するパッチ。</param>
    /// <returns>日本語名をキーとする成果物形式の技辞書。</returns>
    internal static JsonObject ConvertMoves(
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

    /// <summary>アイテム修正子に日本語名（item-name-patch 優先、無ければ翻訳）を当てて成果物形式に変換する。</summary>
    /// <remarks>日本語名が解決できないアイテムは出力から除外する。</remarks>
    /// <param name="itemsModifiers">アイテム修正子定義。</param>
    /// <param name="itemNames">アイテムの日本語名辞書（翻訳）。</param>
    /// <param name="itemNamePatch">アイテム日本語名の上書きパッチ。</param>
    /// <returns>日本語名をキーとする成果物形式のアイテム辞書。</returns>
    internal static JsonObject ConvertItems(
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

    /// <summary>特性修正子に日本語名（翻訳）を当てて成果物形式に変換する。</summary>
    /// <remarks>日本語名が解決できない特性は出力から除外する。</remarks>
    /// <param name="abilitiesModifiers">特性修正子定義。</param>
    /// <param name="abilityNames">特性の日本語名辞書（翻訳）。</param>
    /// <returns>日本語名をキーとする成果物形式の特性辞書。</returns>
    internal static JsonObject ConvertAbilities(
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
