using System.Text.Json.Nodes;
using PokelensTools;
using Xunit;

namespace PokelensTools.Tests;

public class ShowdownFetcherTests
{
    [Fact]
    public void JsToJson_StripsExportsWrapper_AndQuotesUnquotedKeys()
    {
        var js = """
            'use strict';exports.BattlePokedex = {
              pikachu: {num: 25, name: "Pikachu"}
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        Assert.Equal(25, parsed["pikachu"]!["num"]!.GetValue<int>());
        Assert.Equal("Pikachu", parsed["pikachu"]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void JsToJson_QuotesNumericKeys()
    {
        var js = """
            exports.X = {
              pikachu: {abilities: {0: "Static", H: "Lightning Rod"}}
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        var abilities = parsed["pikachu"]!["abilities"]!.AsObject();
        Assert.Equal("Static", abilities["0"]!.GetValue<string>());
        Assert.Equal("Lightning Rod", abilities["H"]!.GetValue<string>());
    }

    [Fact]
    public void JsToJson_DoesNotQuoteArrayElements()
    {
        var js = """
            exports.X = {
              foo: {nums: [2, 5], strs: ["a", "b"]}
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        var nums = parsed["foo"]!["nums"]!.AsArray();
        Assert.Equal(2, nums[0]!.GetValue<int>());
        Assert.Equal(5, nums[1]!.GetValue<int>());
    }

    [Fact]
    public void JsToJson_RemovesTrailingCommas()
    {
        var js = """
            exports.X = {
              a: {hp: 35, atk: 55,},
              b: [1, 2, 3,],
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        Assert.Equal(35, parsed["a"]!["hp"]!.GetValue<int>());
        Assert.Equal(3, parsed["b"]!.AsArray().Count);
    }

    [Fact]
    public void JsToJson_HandlesNestedObjects()
    {
        var js = """
            exports.X = {
              pikachu: {
                num: 25,
                baseStats: {hp: 35, atk: 55, def: 40, spa: 50, spd: 50, spe: 90},
                abilities: {0: "Static", H: "Lightning Rod"}
              }
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        var stats = parsed["pikachu"]!["baseStats"]!.AsObject();
        Assert.Equal(35, stats["hp"]!.GetValue<int>());
        Assert.Equal(90, stats["spe"]!.GetValue<int>());
    }

    [Fact]
    public void JsToJson_HandlesAlreadyQuotedKeys()
    {
        var js = """
            exports.X = {
              "pikachu": {"num": 25, "name": "Pikachu"}
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        Assert.Equal(25, parsed["pikachu"]!["num"]!.GetValue<int>());
    }

    [Fact]
    public void JsToJson_HandlesBooleanValues()
    {
        var js = """
            exports.X = {
              protect: {accuracy: true, basePower: 0}
            };
            """;
        var json = ShowdownFetcher.JsToJson(js);

        var parsed = JsonNode.Parse(json)!.AsObject();
        Assert.True(parsed["protect"]!["accuracy"]!.GetValue<bool>());
        Assert.Equal(0, parsed["protect"]!["basePower"]!.GetValue<int>());
    }

    [Fact]
    public void JsToJson_NoTopLevelObject_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => ShowdownFetcher.JsToJson("'use strict';"));
    }

    // ---------- BuildMoveEntry: isZ / isMax exclusion ----------

    [Fact]
    public void BuildMoveEntry_IsZ_ReturnsNull()
    {
        var entry = new JsonObject
        {
            ["num"] = 628,
            ["name"] = "Acid Downpour",
            ["type"] = "Poison",
            ["category"] = "Physical",
            ["basePower"] = 1,
            ["accuracy"] = JsonValue.Create(true),
            ["flags"] = new JsonObject(),
            ["isZ"] = JsonValue.Create(true),
        };
        Assert.Null(ShowdownFetcher.BuildMoveEntry(entry));
    }

    [Fact]
    public void BuildMoveEntry_IsMax_ReturnsNull()
    {
        var entry = new JsonObject
        {
            ["num"] = 1000,
            ["name"] = "G-Max Fireball",
            ["type"] = "Fire",
            ["category"] = "Physical",
            ["basePower"] = 160,
            ["accuracy"] = JsonValue.Create(true),
            ["flags"] = new JsonObject(),
            ["isMax"] = JsonValue.Create(true),
        };
        Assert.Null(ShowdownFetcher.BuildMoveEntry(entry));
    }

    [Fact]
    public void BuildMoveEntry_StandardMove_ReturnsEntry()
    {
        var entry = new JsonObject
        {
            ["num"] = 85,
            ["name"] = "Thunderbolt",
            ["type"] = "Electric",
            ["category"] = "Special",
            ["basePower"] = 90,
            ["accuracy"] = (JsonNode)100,
            ["flags"] = new JsonObject(),
        };
        var built = ShowdownFetcher.BuildMoveEntry(entry);
        Assert.NotNull(built);
        Assert.Equal(90, built!["basePower"]!.GetValue<int>());
    }

    // ---------- BuildItemEntry / BuildAbilityEntry: isNonstandard exclusion ----------

    [Fact]
    public void BuildItemEntry_IsNonstandard_ReturnsNull()
    {
        var entry = new JsonObject
        {
            ["num"] = 999,
            ["name"] = "Past Berry",
            ["isNonstandard"] = "Past",
        };
        Assert.Null(ShowdownFetcher.BuildItemEntry(entry));
    }

    [Fact]
    public void BuildItemEntry_StandardItem_ReturnsEntry()
    {
        var entry = new JsonObject
        {
            ["num"] = 1,
            ["name"] = "Choice Scarf",
        };
        var built = ShowdownFetcher.BuildItemEntry(entry);
        Assert.NotNull(built);
        Assert.Equal("Choice Scarf", built!["name"]!.GetValue<string>());
    }

    [Fact]
    public void BuildAbilityEntry_IsNonstandard_ReturnsNull()
    {
        var entry = new JsonObject
        {
            ["num"] = 999,
            ["isNonstandard"] = "Future",
        };
        Assert.Null(ShowdownFetcher.BuildAbilityEntry(entry));
    }

    [Fact]
    public void BuildAbilityEntry_StandardAbility_ReturnsEntry()
    {
        var entry = new JsonObject
        {
            ["num"] = 9,
        };
        var built = ShowdownFetcher.BuildAbilityEntry(entry);
        Assert.NotNull(built);
        Assert.Equal(9, built!["num"]!.GetValue<int>());
    }
}
