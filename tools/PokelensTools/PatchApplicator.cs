using System.Text.Json.Nodes;

namespace PokelensTools;

internal static class PatchApplicator
{
    public static void Apply(
        string showdownPokedexPath,
        string showdownMovesPath,
        string championsPatchPath)
    {
        // champions-patch.json はパイプライン構成上、常に存在するべきファイル。
        // 不在やパース失敗をサイレントにスキップすると Step4 が古いキャッシュで進行してしまうため、
        // 明示的に例外として伝播させる。
        if (!File.Exists(championsPatchPath))
        {
            throw new FileNotFoundException(
                $"champions-patch.json が見つかりません: {championsPatchPath}",
                championsPatchPath);
        }

        JsonObject patch = JsonNode.Parse(File.ReadAllText(championsPatchPath))?.AsObject()
            ?? throw new InvalidDataException(
                $"champions-patch.json のパースに失敗しました: {championsPatchPath}");

        ApplyPokedexPatch(showdownPokedexPath, patch["pokedex"]?.AsObject());
        ApplyMovesPatch(showdownMovesPath, patch["moves"]?.AsObject());
    }

    public static void ApplyPokedexPatch(string pokedexPath, JsonObject? patchSection)
    {
        if (patchSection == null)
        {
            return;
        }

        JsonObject pokedex = JsonNode.Parse(File.ReadAllText(pokedexPath))?.AsObject()
            ?? throw new InvalidDataException(
                $"ポケデックスキャッシュのパースに失敗しました: {pokedexPath}");

        foreach (var (key, patchEntry) in patchSection)
        {
            if (patchEntry is not JsonObject changes)
            {
                continue;
            }

            if (!pokedex.TryGetPropertyValue(key, out JsonNode? existing))
            {
                continue;
            }

            if (existing is not JsonObject entry)
            {
                continue;
            }

            if (changes["baseStats"]?.AsObject() is JsonObject patchStats)
            {
                MergeBaseStats(entry, patchStats);
            }

            if (changes["types"] is JsonArray patchTypes)
            {
                MergeTypes(entry, patchTypes);
            }

            if (changes["abilities"]?.AsObject() is JsonObject patchAbilities)
            {
                MergeAbilities(entry, patchAbilities);
            }
        }

        File.WriteAllText(pokedexPath, JsonHelpers.ToIndentedJson(pokedex));
    }

    private static void MergeBaseStats(JsonObject entry, JsonObject patchStats)
    {
        JsonObject? entryStats = entry["baseStats"]?.AsObject();
        if (entryStats == null)
        {
            return;
        }

        foreach (var (stat, val) in patchStats)
        {
            entryStats[stat] = val?.DeepClone();
        }
    }

    private static void MergeTypes(JsonObject entry, JsonArray patchTypes)
    {
        entry["types"] = patchTypes.DeepClone();
    }

    private static void MergeAbilities(JsonObject entry, JsonObject patchAbilities)
    {
        JsonObject? entryAbilities = entry["abilities"]?.AsObject();
        if (entryAbilities == null)
        {
            return;
        }

        foreach (var (slot, val) in patchAbilities)
        {
            entryAbilities[slot] = val?.DeepClone();
        }
    }

    public static void ApplyMovesPatch(string movesPath, JsonObject? patchSection)
    {
        if (patchSection == null)
        {
            return;
        }

        JsonObject moves = JsonNode.Parse(File.ReadAllText(movesPath))?.AsObject()
            ?? throw new InvalidDataException(
                $"技キャッシュのパースに失敗しました: {movesPath}");

        foreach (var (key, patchEntry) in patchSection)
        {
            if (patchEntry is not JsonObject changes)
            {
                continue;
            }

            if (!moves.TryGetPropertyValue(key, out JsonNode? existing))
            {
                continue;
            }

            if (existing is not JsonObject entry)
            {
                continue;
            }

            if (changes["basePower"] is JsonNode basePower)
            {
                entry["basePower"] = basePower.DeepClone();
            }

            if (changes["accuracy"] is JsonNode accuracy)
            {
                entry["accuracy"] = accuracy.DeepClone();
            }

            if (changes["category"] is JsonNode category)
            {
                entry["category"] = category.DeepClone();
            }
        }

        File.WriteAllText(movesPath, JsonHelpers.ToIndentedJson(moves));
    }
}
