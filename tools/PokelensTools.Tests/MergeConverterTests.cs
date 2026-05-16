using System.Text.Json.Nodes;
using PokelensTools;
using Xunit;

namespace PokelensTools.Tests;

public class MergeConverterTests
{
    private static JsonObject MakePokedex() => new()
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
            ["abilities"] = new JsonObject
            {
                ["0"] = "Static",
                ["H"] = "Lightning Rod",
            },
        },
    };

    private static JsonObject MakeMoves() => new()
    {
        ["thunderbolt"] = new JsonObject
        {
            ["num"] = 85, ["name"] = "Thunderbolt",
            ["type"] = "Electric", ["category"] = "Special",
            ["basePower"] = 90, ["accuracy"] = (JsonNode)100,
            ["flags"] = new JsonObject { ["protect"] = 1, ["mirror"] = 1 },
        },
        ["splash"] = new JsonObject
        {
            ["num"] = 150, ["name"] = "Splash",
            ["type"] = "Normal", ["category"] = "Status",
            ["basePower"] = 0, ["accuracy"] = (JsonNode)true,
            ["flags"] = new JsonObject(),
        },
        ["doubleslap"] = new JsonObject
        {
            ["num"] = 3, ["name"] = "DoubleSlap",
            ["type"] = "Normal", ["category"] = "Physical",
            ["basePower"] = 15, ["accuracy"] = (JsonNode)85,
            ["flags"] = new JsonObject { ["contact"] = 1, ["punch"] = 1 },
            ["multihit"] = new JsonArray { 2, 5 },
        },
        ["doubleedge"] = new JsonObject
        {
            ["num"] = 38, ["name"] = "Double-Edge",
            ["type"] = "Normal", ["category"] = "Physical",
            ["basePower"] = 120, ["accuracy"] = (JsonNode)100,
            ["flags"] = new JsonObject { ["contact"] = 1 },
            ["recoil"] = true,
        },
        ["metronome"] = new JsonObject
        {
            ["num"] = 118, ["name"] = "Metronome",
            ["type"] = "Normal", ["category"] = "Status",
            ["basePower"] = 0, ["accuracy"] = (JsonNode)true,
            ["flags"] = new JsonObject(),
        },
    };

    private static JsonObject MakeMoveNames() => new()
    {
        ["thunderbolt"] = "10まんボルト",
        ["splash"] = "はねる",
        ["doubleslap"] = "はたきおとす",
        ["doubleedge"] = "のしかかり",
        ["metronome"] = "メトロノーム",
    };

    private static JsonObject MakePokeapiNames() => new()
    {
        ["pikachu"] = "ピカチュウ",
    };

    private static JsonObject MakeAbilityNames() => new()
    {
        ["static"] = "せいでんき",
        ["lightningrod"] = "ひらいしん",
    };

    // ---------- FlagToTag ----------

    [Fact]
    public void FlagToTag_GenericFlag_IsPrefix()
    {
        Assert.Equal("isContact", MergeConverter.FlagToTag("contact"));
        Assert.Equal("isPunch", MergeConverter.FlagToTag("punch"));
        Assert.Equal("isPulse", MergeConverter.FlagToTag("pulse"));
        Assert.Equal("isBite", MergeConverter.FlagToTag("bite"));
        Assert.Equal("isProtect", MergeConverter.FlagToTag("protect"));
    }

    [Fact]
    public void FlagToTag_Slicing_ReturnsIsSlice()
    {
        Assert.Equal("isSlice", MergeConverter.FlagToTag("slicing"));
    }

    // ---------- ConvertPokedex ----------

    [Fact]
    public void ConvertPokedex_JapaneseName()
    {
        var result = MergeConverter.ConvertPokedex(MakePokedex(), MakePokeapiNames(), MakeAbilityNames(), new JsonObject());
        Assert.Equal("ピカチュウ", result["pikachu"]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void ConvertPokedex_Abilities_JapaneseAndSlotOrder()
    {
        var result = MergeConverter.ConvertPokedex(MakePokedex(), MakePokeapiNames(), MakeAbilityNames(), new JsonObject());
        var abilities = result["pikachu"]!["abilities"]!.AsArray();
        // Slot "0" = Static → "せいでんき", slot "1" absent, slot "H" = Lightning Rod → "ひらいしん"
        Assert.Equal(2, abilities.Count);
        Assert.Equal("せいでんき", abilities[0]!.GetValue<string>());
        Assert.Equal("ひらいしん", abilities[1]!.GetValue<string>());
    }

    [Fact]
    public void ConvertPokedex_NoTranslation_EntrySkipped()
    {
        var pokedex = new JsonObject
        {
            ["unknown"] = new JsonObject
            {
                ["num"] = 9999, ["name"] = "Unknown",
                ["types"] = new JsonArray { "Normal" },
                ["baseStats"] = new JsonObject { ["hp"]=1,["atk"]=1,["def"]=1,["spa"]=1,["spd"]=1,["spe"]=1 },
                ["abilities"] = new JsonObject(),
            },
        };
        var result = MergeConverter.ConvertPokedex(pokedex, new JsonObject(), new JsonObject(), new JsonObject());
        Assert.Empty(result);
    }

    [Fact]
    public void ConvertPokedex_NamePatch_OverridesTranslation()
    {
        var patch = new JsonObject { ["pikachu"] = "パートナーピカチュウ" };
        var result = MergeConverter.ConvertPokedex(MakePokedex(), MakePokeapiNames(), MakeAbilityNames(), patch);
        Assert.Equal("パートナーピカチュウ", result["pikachu"]!["name"]!.GetValue<string>());
    }

    // ---------- ConvertMoves ----------

    [Fact]
    public void ConvertMoves_ZMoveAndMaxMove_Excluded()
    {
        var moves = new JsonObject
        {
            ["aciddownpour"] = new JsonObject
            {
                ["num"] = 628, ["name"] = "Acid Downpour",
                ["type"] = "Poison", ["category"] = "Physical",
                ["basePower"] = 1, ["accuracy"] = JsonValue.Create(true),
                ["flags"] = new JsonObject(),
                ["isZ"] = JsonValue.Create(true),
            },
            ["gmaxfireball"] = new JsonObject
            {
                ["num"] = 1000, ["name"] = "G-Max Fireball",
                ["type"] = "Fire", ["category"] = "Physical",
                ["basePower"] = 160, ["accuracy"] = JsonValue.Create(true),
                ["flags"] = new JsonObject(),
                ["isMax"] = JsonValue.Create(true),
            },
            ["thunderbolt"] = new JsonObject
            {
                ["num"] = 85, ["name"] = "Thunderbolt",
                ["type"] = "Electric", ["category"] = "Special",
                ["basePower"] = 90, ["accuracy"] = (JsonNode)100,
                ["flags"] = new JsonObject(),
            },
        };
        var names = new JsonObject
        {
            ["aciddownpour"] = "アシッドポイズンデリート",
            ["gmaxfireball"] = "キョダイホノオダマ",
            ["thunderbolt"] = "10まんボルト",
        };
        var result = MergeConverter.ConvertMoves(moves, names, new JsonObject());
        Assert.False(result.ContainsKey("アシッドポイズンデリート"));
        Assert.False(result.ContainsKey("キョダイホノオダマ"));
        Assert.True(result.ContainsKey("10まんボルト"));
    }

    [Fact]
    public void ConvertMoves_BasePower0_PowerNull()
    {
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        Assert.Null(result["はねる"]!["power"]?.GetValue<int?>());
    }

    [Fact]
    public void ConvertMoves_AccuracyTrue_AccuracyNull()
    {
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        Assert.Null(result["はねる"]!["accuracy"]?.GetValue<int?>());
    }

    [Fact]
    public void ConvertMoves_Multihit_PowerIsMaxHitsTimesBase()
    {
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        // doubleslap: basePower=15, multihit=[2,5] → power = 15 * 5 = 75
        Assert.Equal(75, result["はたきおとす"]!["power"]!.GetValue<int>());
    }

    [Fact]
    public void ConvertMoves_MultihitSingleInteger_PowerIsHitsTimesBase()
    {
        var moves = new JsonObject
        {
            ["tripleaxel"] = new JsonObject
            {
                ["num"] = 813, ["name"] = "Triple Axel",
                ["type"] = "Ice", ["category"] = "Physical",
                ["basePower"] = 20, ["accuracy"] = (JsonNode)90,
                ["flags"] = new JsonObject { ["contact"] = 1 },
                ["multihit"] = 3,
            },
        };
        var names = new JsonObject { ["tripleaxel"] = "トリプルアクセル" };

        var result = MergeConverter.ConvertMoves(moves, names, new JsonObject());

        // basePower=20, multihit=3 (single int) → power = 20 * 3 = 60
        Assert.Equal(60, result["トリプルアクセル"]!["power"]!.GetValue<int>());
    }

    [Fact]
    public void ConvertMoves_MovesPowerPatch_NullPowerOverwritten()
    {
        var patch = new JsonObject { ["metronome"] = new JsonObject { ["power"] = 120 } };
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), patch);
        Assert.Equal(120, result["メトロノーム"]!["power"]!.GetValue<int>());
    }

    [Fact]
    public void ConvertMoves_FlagsConvertedToTags()
    {
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        var tags = result["10まんボルト"]!["tags"]!.AsArray()
            .Select(t => t!.GetValue<string>()).ToList();
        Assert.Contains("isProtect", tags);
        Assert.Contains("isMirror", tags);
    }

    [Fact]
    public void ConvertMoves_RecoilField_AddsIsRecoilTag()
    {
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        var tags = result["のしかかり"]!["tags"]!.AsArray()
            .Select(t => t!.GetValue<string>()).ToList();
        Assert.Contains("isRecoil", tags);
    }

    [Fact]
    public void ConvertMoves_NoFlags_TagsFieldAbsent()
    {
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        // "はねる" (splash) has no flags and no recoil
        Assert.Null(result["はねる"]!["tags"]);
    }

    // ---------- ConvertItems ----------

    [Fact]
    public void ConvertItems_JapaneseKey()
    {
        var modifiers = new JsonObject
        {
            ["choicescarf"] = new JsonObject { ["modifier"] = new JsonObject { ["spe"] = 1.5 } },
        };
        var itemNames = new JsonObject { ["choicescarf"] = "こだわりスカーフ" };

        var result = MergeConverter.ConvertItems(modifiers, itemNames, new JsonObject());

        Assert.True(result.ContainsKey("こだわりスカーフ"));
        Assert.False(result.ContainsKey("choicescarf"));
    }

    [Fact]
    public void ConvertItems_NoTranslation_Excluded()
    {
        var modifiers = new JsonObject
        {
            ["unknown_item"] = new JsonObject { ["modifier"] = new JsonObject { ["atk"] = 1.5 } },
        };
        var result = MergeConverter.ConvertItems(modifiers, new JsonObject(), new JsonObject());
        Assert.Empty(result);
    }

    [Fact]
    public void ConvertItems_NamePatch_OverridesTranslation()
    {
        var modifiers = new JsonObject
        {
            ["masterpieceteacup"] = new JsonObject { ["modifier"] = new JsonObject() },
        };
        var itemNames = new JsonObject { ["masterpieceteacup"] = "ボンサクのちゃわん" };
        var patch = new JsonObject { ["masterpieceteacup"] = "ケッサクのちゃわん" };
        var result = MergeConverter.ConvertItems(modifiers, itemNames, patch);
        Assert.True(result.ContainsKey("ケッサクのちゃわん"));
        Assert.False(result.ContainsKey("ボンサクのちゃわん"));
    }

    [Fact]
    public void ConvertItems_NamePatch_FillsMissingTranslation()
    {
        var modifiers = new JsonObject
        {
            ["metalalloy"] = new JsonObject { ["modifier"] = new JsonObject() },
        };
        var patch = new JsonObject { ["metalalloy"] = "ふくごうきんぞく" };
        var result = MergeConverter.ConvertItems(modifiers, new JsonObject(), patch);
        Assert.True(result.ContainsKey("ふくごうきんぞく"));
    }

    // ---------- ConvertAbilities ----------

    [Fact]
    public void ConvertAbilities_JapaneseKey()
    {
        var modifiers = new JsonObject
        {
            ["ironfist"] = new JsonObject { ["modifier"] = new JsonObject { ["condition"] = "isPunch", ["atk"] = 1.2 } },
        };
        var abilityNames = new JsonObject { ["ironfist"] = "てつのこぶし" };

        var result = MergeConverter.ConvertAbilities(modifiers, abilityNames);

        Assert.True(result.ContainsKey("てつのこぶし"));
        Assert.False(result.ContainsKey("ironfist"));
    }

    [Fact]
    public void ConvertAbilities_NoTranslation_Excluded()
    {
        var modifiers = new JsonObject
        {
            ["unknown_ability"] = new JsonObject { ["modifier"] = new JsonObject() },
        };
        var result = MergeConverter.ConvertAbilities(modifiers, new JsonObject());
        Assert.Empty(result);
    }
}
