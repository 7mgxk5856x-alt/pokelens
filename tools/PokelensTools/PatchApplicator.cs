using System.Text.Json.Nodes;

namespace PokelensTools;

public static class PatchApplicator
{

    public static void Apply(
        string showdownPokedexPath,
        string showdownMovesPath,
        string championsPatchPath)
    {
        if (!File.Exists(championsPatchPath)) return;

        var patch = JsonNode.Parse(File.ReadAllText(championsPatchPath))?.AsObject();
        if (patch == null) return;

        ApplyPokedexPatch(showdownPokedexPath, patch["pokedex"]?.AsObject());
        ApplyMovesPatch(showdownMovesPath, patch["moves"]?.AsObject());
    }

    public static void ApplyPokedexPatch(string pokedexPath, JsonObject? patchSection)
    {
        if (patchSection == null) return;

        var pokedex = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();

        foreach (var (key, patchEntry) in patchSection)
        {
            if (patchEntry is not JsonObject changes) continue;
            if (!pokedex.TryGetPropertyValue(key, out var existing)) continue;
            if (existing is not JsonObject entry) continue;

            if (changes["baseStats"]?.AsObject() is JsonObject patchStats)
            {
                var entryStats = entry["baseStats"]?.AsObject();
                if (entryStats != null)
                {
                    foreach (var (stat, val) in patchStats)
                    {
                        entryStats[stat] = val?.DeepClone();
                    }
                }
            }

            if (changes["types"] is JsonArray patchTypes)
                entry["types"] = patchTypes.DeepClone();

            if (changes["abilities"]?.AsObject() is JsonObject patchAbilities)
            {
                var entryAbilities = entry["abilities"]?.AsObject();
                if (entryAbilities != null)
                {
                    foreach (var (slot, val) in patchAbilities)
                    {
                        entryAbilities[slot] = val?.DeepClone();
                    }
                }
            }
        }

        File.WriteAllText(pokedexPath, JsonHelpers.ToIndentedJson(pokedex));
    }

    public static void ApplyMovesPatch(string movesPath, JsonObject? patchSection)
    {
        if (patchSection == null) return;

        var moves = JsonNode.Parse(File.ReadAllText(movesPath))!.AsObject();

        foreach (var (key, patchEntry) in patchSection)
        {
            if (patchEntry is not JsonObject changes) continue;
            if (!moves.TryGetPropertyValue(key, out var existing)) continue;
            if (existing is not JsonObject entry) continue;

            if (changes["basePower"] is JsonNode basePower)
                entry["basePower"] = basePower.DeepClone();

            if (changes["accuracy"] is JsonNode accuracy)
                entry["accuracy"] = accuracy.DeepClone();

            if (changes["category"] is JsonNode category)
                entry["category"] = category.DeepClone();
        }

        File.WriteAllText(movesPath, JsonHelpers.ToIndentedJson(moves));
    }
}
