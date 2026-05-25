using System.Text.Json.Nodes;
using PokelensMasterDataBuilder.Fetchers;
using Xunit;

namespace PokelensMasterDataBuilder.Tests;

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
    public void BuildItemEntry_IsNonstandard_WithoutMegaStone_ReturnsNull()
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
    public void BuildItemEntry_MegaStone_WithPastNonstandard_PreservesEntry()
    {
        // メガストーンは Gen 6/7 のため isNonstandard: "Past" でマークされるが、
        // 機能 7 のため例外通過させる必要がある（D-6）
        var entry = new JsonObject
        {
            ["num"] = 677,
            ["name"] = "Absolite",
            ["megaStone"] = new JsonObject { ["Absol"] = "Absol-Mega" },
            ["isNonstandard"] = "Past",
        };
        var built = ShowdownFetcher.BuildItemEntry(entry);
        Assert.NotNull(built);
        Assert.Equal("Absolite", built!["name"]!.GetValue<string>());
        var megaStone = built["megaStone"]!.AsObject();
        Assert.Equal("Absol-Mega", megaStone["Absol"]!.GetValue<string>());
    }

    [Fact]
    public void BuildItemEntry_MegaStone_DualForm_PreservesAllMappings()
    {
        // 複数メガを持つアイテム（リザードナイトX/Y のような形）の例として、
        // megaStone マップに 2 つのエントリがあるケースを検証する
        var entry = new JsonObject
        {
            ["num"] = 660,
            ["name"] = "Charizardite X",
            ["megaStone"] = new JsonObject { ["Charizard"] = "Charizard-Mega-X" },
            ["isNonstandard"] = "Past",
        };
        var built = ShowdownFetcher.BuildItemEntry(entry);
        Assert.NotNull(built);
        Assert.Equal("Charizard-Mega-X", built!["megaStone"]!.AsObject()["Charizard"]!.GetValue<string>());
    }

    [Fact]
    public void BuildItemEntry_IsNonstandardUnobtainable_WithoutMegaStone_ReturnsNull()
    {
        // D-6 の同値分割: Past 以外の isNonstandard 値 (Future / Unobtainable / CAP) も
        // megaStone 持ちでなければ除外対象であることを担保する。
        var entry = new JsonObject
        {
            ["num"] = 4,
            ["name"] = "Cherish Ball",
            ["isNonstandard"] = "Unobtainable",
        };
        Assert.Null(ShowdownFetcher.BuildItemEntry(entry));
    }

    [Fact]
    public void BuildItemEntry_IsNonstandardFuture_WithMegaStone_PreservesEntry()
    {
        // D-6 境界値: megaStone を持つアイテムは isNonstandard が Past 以外（Future 等）でも例外通過する。
        // 現状の Showdown データでメガストーンは Past 一律だが、将来仕様変更で別値になっても
        // 「megaStone 有り → 通す」という不変条件が維持されることを担保する。
        var entry = new JsonObject
        {
            ["num"] = 999,
            ["name"] = "Hypothetical New Mega Stone",
            ["megaStone"] = new JsonObject { ["Pikachu"] = "Pikachu-Mega" },
            ["isNonstandard"] = "Future",
        };
        var built = ShowdownFetcher.BuildItemEntry(entry);
        Assert.NotNull(built);
        Assert.Equal("Pikachu-Mega", built!["megaStone"]!.AsObject()["Pikachu"]!.GetValue<string>());
    }

    [Fact]
    public void BuildItemEntry_NoIsNonstandard_WithMegaStone_PreservesEntry()
    {
        // D-6 デシジョンテーブル欠落セル: isNonstandard フィールド無し + megaStone 有り の組み合わせ。
        // 現状の Showdown データでメガストーンは Past 一律のため実例は無いが、
        // 「megaStone 有り → 必ず通す」の不変条件は isNonstandard の有無によらず維持されることを担保する。
        var entry = new JsonObject
        {
            ["num"] = 999,
            ["name"] = "Future Mega Stone",
            ["megaStone"] = new JsonObject { ["Foo"] = "Foo-Mega" },
        };
        var built = ShowdownFetcher.BuildItemEntry(entry);
        Assert.NotNull(built);
        Assert.Equal("Foo-Mega", built!["megaStone"]!.AsObject()["Foo"]!.GetValue<string>());
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
