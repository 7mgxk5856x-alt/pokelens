using System.Text.Json.Nodes;
using PokelensTools.Pipeline;
using Xunit;

namespace PokelensTools.Tests;

public class PatchApplicatorTests
{
    private static JsonObject MakeSamplePokedex() => new()
    {
        ["pikachu"] = new JsonObject
        {
            ["num"] = 25,
            ["name"] = "Pikachu",
            ["types"] = new JsonArray { "Electric" },
            ["baseStats"] = new JsonObject
            {
                ["hp"] = 35, ["atk"] = 55, ["def"] = 40,
                ["spa"] = 50, ["spd"] = 50, ["spe"] = 90,
            },
            ["abilities"] = new JsonObject { ["0"] = "Static", ["H"] = "Lightning Rod" },
        },
    };

    private static JsonObject MakeSampleMoves() => new()
    {
        ["thunderbolt"] = new JsonObject
        {
            ["num"] = 85,
            ["name"] = "Thunderbolt",
            ["type"] = "Electric",
            ["category"] = "Special",
            ["basePower"] = 90,
            ["accuracy"] = 100,
            ["flags"] = new JsonObject(),
        },
    };

    [Fact]
    public void ApplyPokedexPatch_BaseStats_PartialOverwrite()
    {
        var pokedexPath = WriteTempJson(MakeSamplePokedex());
        var patch = new JsonObject
        {
            ["pikachu"] = new JsonObject
            {
                ["baseStats"] = new JsonObject { ["atk"] = 99, ["spe"] = 110 },
            },
        };

        PatchApplicator.ApplyPokedexPatch(pokedexPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();
        var stats = result["pikachu"]!["baseStats"]!.AsObject();
        Assert.Equal(35, stats["hp"]!.GetValue<int>());  // unchanged
        Assert.Equal(99, stats["atk"]!.GetValue<int>()); // patched
        Assert.Equal(40, stats["def"]!.GetValue<int>());  // unchanged
        Assert.Equal(110, stats["spe"]!.GetValue<int>()); // patched

        File.Delete(pokedexPath);
    }

    [Fact]
    public void ApplyPokedexPatch_Types_Overwrite()
    {
        var pokedexPath = WriteTempJson(MakeSamplePokedex());
        var patch = new JsonObject
        {
            ["pikachu"] = new JsonObject
            {
                ["types"] = new JsonArray { "Electric", "Steel" },
            },
        };

        PatchApplicator.ApplyPokedexPatch(pokedexPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();
        var types = result["pikachu"]!["types"]!.AsArray();
        Assert.Equal(2, types.Count);
        Assert.Equal("Electric", types[0]!.GetValue<string>());
        Assert.Equal("Steel", types[1]!.GetValue<string>());

        File.Delete(pokedexPath);
    }

    [Fact]
    public void ApplyPokedexPatch_Abilities_PartialOverwrite()
    {
        var pokedexPath = WriteTempJson(MakeSamplePokedex());
        var patch = new JsonObject
        {
            ["pikachu"] = new JsonObject
            {
                ["abilities"] = new JsonObject
                {
                    ["0"] = "Surge Surfer",  // override existing slot
                    ["1"] = "Hidden Power",  // add new slot
                },
            },
        };

        PatchApplicator.ApplyPokedexPatch(pokedexPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();
        var abilities = result["pikachu"]!["abilities"]!.AsObject();
        Assert.Equal("Surge Surfer", abilities["0"]!.GetValue<string>());     // patched
        Assert.Equal("Hidden Power", abilities["1"]!.GetValue<string>());     // added
        Assert.Equal("Lightning Rod", abilities["H"]!.GetValue<string>());    // unchanged

        File.Delete(pokedexPath);
    }

    [Fact]
    public void ApplyPokedexPatch_UnspecifiedFields_Unchanged()
    {
        var pokedexPath = WriteTempJson(MakeSamplePokedex());
        var patch = new JsonObject
        {
            ["pikachu"] = new JsonObject
            {
                ["baseStats"] = new JsonObject { ["atk"] = 99 },
            },
        };

        PatchApplicator.ApplyPokedexPatch(pokedexPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();
        var stats = result["pikachu"]!["baseStats"]!.AsObject();
        Assert.Equal(35, stats["hp"]!.GetValue<int>()); // unchanged

        File.Delete(pokedexPath);
    }

    [Fact]
    public void ApplyMovesPatch_BasePower_Overwrite()
    {
        var movesPath = WriteTempJson(MakeSampleMoves());
        var patch = new JsonObject
        {
            ["thunderbolt"] = new JsonObject { ["basePower"] = 110 },
        };

        PatchApplicator.ApplyMovesPatch(movesPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(movesPath))!.AsObject();
        Assert.Equal(110, result["thunderbolt"]!["basePower"]!.GetValue<int>());

        File.Delete(movesPath);
    }

    [Fact]
    public void ApplyMovesPatch_Accuracy_Overwrite()
    {
        var movesPath = WriteTempJson(MakeSampleMoves());
        var patch = new JsonObject
        {
            ["thunderbolt"] = new JsonObject { ["accuracy"] = 85 },
        };

        PatchApplicator.ApplyMovesPatch(movesPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(movesPath))!.AsObject();
        Assert.Equal(85, result["thunderbolt"]!["accuracy"]!.GetValue<int>()); // patched
        Assert.Equal(90, result["thunderbolt"]!["basePower"]!.GetValue<int>()); // unchanged

        File.Delete(movesPath);
    }

    [Fact]
    public void ApplyMovesPatch_Category_Overwrite()
    {
        var movesPath = WriteTempJson(MakeSampleMoves());
        var patch = new JsonObject
        {
            ["thunderbolt"] = new JsonObject { ["category"] = "Physical" },
        };

        PatchApplicator.ApplyMovesPatch(movesPath, patch);

        var result = JsonNode.Parse(File.ReadAllText(movesPath))!.AsObject();
        Assert.Equal("Physical", result["thunderbolt"]!["category"]!.GetValue<string>()); // patched
        Assert.Equal("Electric", result["thunderbolt"]!["type"]!.GetValue<string>());     // unchanged

        File.Delete(movesPath);
    }

    [Fact]
    public void ApplyPokedexPatch_NullPatch_NoChanges()
    {
        var pokedexPath = WriteTempJson(MakeSamplePokedex());
        var originalContent = File.ReadAllText(pokedexPath);

        PatchApplicator.ApplyPokedexPatch(pokedexPath, null);

        // File should not be modified
        Assert.Equal(originalContent, File.ReadAllText(pokedexPath));
        File.Delete(pokedexPath);
    }

    private static string WriteTempJson(JsonObject obj)
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        using var ms = new System.IO.MemoryStream();
        using var writer = new System.Text.Json.Utf8JsonWriter(ms);
        obj.WriteTo(writer);
        writer.Flush();
        File.WriteAllBytes(path, ms.ToArray());
        return path;
    }
}
