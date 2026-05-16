using System.Text.Json.Nodes;

namespace PokelensTools;

public static class MergeConverter
{
    // Map of Showdown flag keys → JSON tag names (exceptions first, then generic rule)
    private static readonly Dictionary<string, string> FlagExceptions = new()
    {
        ["slicing"] = "isSlice",
    };

    public static string FlagToTag(string flag)
    {
        if (FlagExceptions.TryGetValue(flag, out var exception)) return exception;
        return "is" + char.ToUpperInvariant(flag[0]) + flag[1..];
    }

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

        var pokedex = JsonNode.Parse(File.ReadAllText(showdownPokedexPath))!.AsObject();
        var moves = JsonNode.Parse(File.ReadAllText(showdownMovesPath))!.AsObject();
        var items = JsonNode.Parse(File.ReadAllText(showdownItemsPath))!.AsObject();
        var abilities = JsonNode.Parse(File.ReadAllText(showdownAbilitiesPath))!.AsObject();
        var translations = JsonNode.Parse(File.ReadAllText(translationsPath))!.AsObject();
        var movesPowerPatch = File.Exists(movesPowerPatchPath)
            ? JsonNode.Parse(File.ReadAllText(movesPowerPatchPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        var itemsModifiers = File.Exists(itemsModifiersPath)
            ? JsonNode.Parse(File.ReadAllText(itemsModifiersPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        var abilitiesModifiers = File.Exists(abilitiesModifiersPath)
            ? JsonNode.Parse(File.ReadAllText(abilitiesModifiersPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        var pokemonNamePatch = File.Exists(pokemonNamePatchPath)
            ? JsonNode.Parse(File.ReadAllText(pokemonNamePatchPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        var itemNamePatch = File.Exists(itemNamePatchPath)
            ? JsonNode.Parse(File.ReadAllText(itemNamePatchPath))?.AsObject() ?? new JsonObject()
            : new JsonObject();

        var pokemonNames = translations["pokemon"]?.AsObject() ?? new JsonObject();
        var moveNames = translations["moves"]?.AsObject() ?? new JsonObject();
        var abilityNames = translations["abilities"]?.AsObject() ?? new JsonObject();
        var itemNames = translations["items"]?.AsObject() ?? new JsonObject();

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

    public static JsonObject ConvertPokedex(
        JsonObject showdownPokedex,
        JsonObject pokemonNames,
        JsonObject abilityNames,
        JsonObject pokemonNamePatch)
    {
        var result = new JsonObject();

        foreach (var (key, val) in showdownPokedex)
        {
            if (val is not JsonObject entry) continue;

            if (!pokemonNames.TryGetPropertyValue(key, out var nameNode)) continue;
            var jaName = nameNode?.GetValue<string>();
            if (string.IsNullOrEmpty(jaName)) continue;

            if (pokemonNamePatch.TryGetPropertyValue(key, out var patchNode)
                && patchNode is JsonValue patchVal
                && patchVal.TryGetValue<string>(out var patchName)
                && !string.IsNullOrEmpty(patchName))
            {
                jaName = patchName;
            }

            var abilitiesSlots = entry["abilities"]?.AsObject();
            var abilitiesList = new JsonArray();
            foreach (var slot in new[] { "0", "1", "H" })
            {
                if (abilitiesSlots?[slot] is JsonNode slotVal)
                {
                    var engKey = slotVal.GetValue<string>()?.ToLowerInvariant()
                        .Replace(" ", "").Replace("-", "");
                    if (engKey != null && abilityNames.TryGetPropertyValue(engKey, out var jaAbility))
                        abilitiesList.Add(jaAbility?.GetValue<string>());
                    else
                        abilitiesList.Add(slotVal.GetValue<string>());
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

    public static JsonObject ConvertMoves(
        JsonObject showdownMoves,
        JsonObject moveNames,
        JsonObject movesPowerPatch)
    {
        var result = new JsonObject();

        foreach (var (key, val) in showdownMoves)
        {
            if (val is not JsonObject entry) continue;
            if (entry["isZ"] != null || entry["isMax"] != null) continue;

            if (!moveNames.TryGetPropertyValue(key, out var nameNode)) continue;
            var jaName = nameNode?.GetValue<string>();
            if (string.IsNullOrEmpty(jaName)) continue;

            var basePower = entry["basePower"]?.GetValue<int>() ?? 0;
            var accuracyNode = entry["accuracy"];
            var flags = entry["flags"]?.AsObject();
            var multihit = entry["multihit"];
            var hasRecoil = entry["recoil"]?.GetValue<bool>() == true;

            // Compute power
            int? power = basePower == 0 ? null : basePower;
            if (power != null && multihit != null)
            {
                int maxHits = multihit is JsonArray arr && arr.Count >= 2
                    ? arr[1]?.GetValue<int>() ?? 1
                    : multihit.GetValue<int>();
                power = basePower * maxHits;
            }

            // Apply moves-power-patch for null power moves
            if (power == null && movesPowerPatch.TryGetPropertyValue(key, out var patchEntry))
            {
                var patchPower = patchEntry?["power"]?.GetValue<int>();
                if (patchPower != null) power = patchPower;
            }

            // Compute accuracy (true → null)
            int? accuracy = null;
            if (accuracyNode is JsonValue accVal)
            {
                if (accVal.TryGetValue<int>(out var accInt)) accuracy = accInt;
                // accuracy: true → null (must-hit)
            }

            // Compute tags from flags + recoil
            var tags = new List<string>();
            if (flags != null)
            {
                foreach (var (flag, _) in flags)
                    tags.Add(FlagToTag(flag));
            }
            if (hasRecoil) tags.Add("isRecoil");

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
                foreach (var tag in tags) tagsArray.Add(tag);
                moveEntry["tags"] = tagsArray;
            }

            result[jaName] = moveEntry;
        }

        return result;
    }

    public static JsonObject ConvertItems(
        JsonObject itemsModifiers,
        JsonObject itemNames,
        JsonObject itemNamePatch)
    {
        var result = new JsonObject();

        foreach (var (showdownKey, modifierEntry) in itemsModifiers)
        {
            string? jaName = null;
            if (itemNamePatch.TryGetPropertyValue(showdownKey, out var patchNode)
                && patchNode is JsonValue patchVal
                && patchVal.TryGetValue<string>(out var patchName)
                && !string.IsNullOrEmpty(patchName))
            {
                jaName = patchName;
            }
            else if (itemNames.TryGetPropertyValue(showdownKey, out var nameNode))
            {
                jaName = nameNode?.GetValue<string>();
            }
            if (string.IsNullOrEmpty(jaName)) continue;
            result[jaName] = modifierEntry?.DeepClone();
        }

        return result;
    }

    public static JsonObject ConvertAbilities(
        JsonObject abilitiesModifiers,
        JsonObject abilityNames)
    {
        var result = new JsonObject();

        foreach (var (showdownKey, modifierEntry) in abilitiesModifiers)
        {
            if (!abilityNames.TryGetPropertyValue(showdownKey, out var nameNode)) continue;
            var jaName = nameNode?.GetValue<string>();
            if (string.IsNullOrEmpty(jaName)) continue;
            result[jaName] = modifierEntry?.DeepClone();
        }

        return result;
    }
}
