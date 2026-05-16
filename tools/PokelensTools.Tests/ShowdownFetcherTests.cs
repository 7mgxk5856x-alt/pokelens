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
}
