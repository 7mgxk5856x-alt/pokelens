using System.Text.Json.Nodes;
using PokelensTools.Pipeline;
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
    // Note: Zワザ・ダイマックスワザの除外は ShowdownFetcher.FetchMovesAsync (Step1) の責務に
    // 移ったため、MergeConverter 側ではフィルタしない。ここではテストしない。

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

    [Fact]
    public void ConvertMoves_SecondaryObject_AddsHasSecondaryTag()
    {
        var moves = new JsonObject
        {
            ["flamethrower"] = new JsonObject
            {
                ["num"] = 53, ["name"] = "Flamethrower",
                ["type"] = "Fire", ["category"] = "Special",
                ["basePower"] = 90, ["accuracy"] = (JsonNode)100,
                ["flags"] = new JsonObject(),
                ["secondary"] = new JsonObject { ["chance"] = 10, ["status"] = "brn" },
            },
        };
        var names = new JsonObject { ["flamethrower"] = "かえんほうしゃ" };

        var result = MergeConverter.ConvertMoves(moves, names, new JsonObject());
        var tags = result["かえんほうしゃ"]!["tags"]!.AsArray()
            .Select(t => t!.GetValue<string>()).ToList();
        Assert.Contains("hasSecondary", tags);
    }

    [Fact]
    public void ConvertMoves_SecondariesArray_AddsHasSecondaryTag()
    {
        var moves = new JsonObject
        {
            ["firefang"] = new JsonObject
            {
                ["num"] = 424, ["name"] = "Fire Fang",
                ["type"] = "Fire", ["category"] = "Physical",
                ["basePower"] = 65, ["accuracy"] = (JsonNode)95,
                ["flags"] = new JsonObject { ["bite"] = 1 },
                ["secondaries"] = new JsonArray
                {
                    new JsonObject { ["chance"] = 10, ["status"] = "brn" },
                    new JsonObject { ["chance"] = 10, ["volatileStatus"] = "flinch" },
                },
            },
        };
        var names = new JsonObject { ["firefang"] = "ほのおのキバ" };

        var result = MergeConverter.ConvertMoves(moves, names, new JsonObject());
        var tags = result["ほのおのキバ"]!["tags"]!.AsArray()
            .Select(t => t!.GetValue<string>()).ToList();
        Assert.Contains("hasSecondary", tags);
    }

    [Fact]
    public void ConvertMoves_NoSecondary_NoHasSecondaryTag()
    {
        // thunderbolt は secondary 含まない MakeMoves 定義
        var result = MergeConverter.ConvertMoves(MakeMoves(), MakeMoveNames(), new JsonObject());
        var tags = result["10まんボルト"]!["tags"]!.AsArray()
            .Select(t => t!.GetValue<string>()).ToList();
        // 肯定側: flags={protect, mirror} は正しくタグへ変換されている
        Assert.Contains("isProtect", tags);
        // 否定側: secondary が無いので hasSecondary は付与されない
        Assert.DoesNotContain("hasSecondary", tags);
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

    // ---------- NestMegaForms (機能 7) ----------

    // 親 + メガ独立エントリを併せ持つフラットな pokedex を返す。
    // ConvertPokedex 完了後の状態を模擬する。
    private static JsonObject MakeFlatPokedexWithMega() => new()
    {
        ["venusaur"] = new JsonObject
        {
            ["num"] = 3,
            ["name"] = "フシギバナ",
            ["types"] = new JsonArray { "Grass", "Poison" },
            ["baseStats"] = new JsonObject { ["hp"]=80,["atk"]=82,["def"]=83,["spa"]=100,["spd"]=100,["spe"]=80 },
            ["abilities"] = new JsonArray { "しんりょく", "ようりょくそ" },
        },
        ["venusaurmega"] = new JsonObject
        {
            ["num"] = 3,
            ["name"] = "メガフシギバナ",
            ["types"] = new JsonArray { "Grass", "Poison" },
            ["baseStats"] = new JsonObject { ["hp"]=80,["atk"]=100,["def"]=123,["spa"]=122,["spd"]=120,["spe"]=80 },
            ["abilities"] = new JsonArray { "あついしぼう" },
        },
        ["charizard"] = new JsonObject
        {
            ["num"] = 6,
            ["name"] = "リザードン",
            ["types"] = new JsonArray { "Fire", "Flying" },
            ["baseStats"] = new JsonObject { ["hp"]=78,["atk"]=84,["def"]=78,["spa"]=109,["spd"]=85,["spe"]=100 },
            ["abilities"] = new JsonArray { "もうか", "サンパワー" },
        },
        ["charizardmegax"] = new JsonObject
        {
            ["num"] = 6,
            // D-8 の全角正規化を X 側でも対称的に検証するため、入力 name を半角 X で記載する
            ["name"] = "メガリザードンX",
            ["types"] = new JsonArray { "Fire", "Dragon" },
            ["baseStats"] = new JsonObject { ["hp"]=78,["atk"]=130,["def"]=111,["spa"]=130,["spd"]=85,["spe"]=100 },
            ["abilities"] = new JsonArray { "かたいツメ" },
        },
        ["charizardmegay"] = new JsonObject
        {
            ["num"] = 6,
            // D-8 の全角正規化を Y 側でも検証するため、入力 name を半角 Y で記載する
            ["name"] = "メガリザードンY",
            ["types"] = new JsonArray { "Fire", "Flying" },
            ["baseStats"] = new JsonObject { ["hp"]=78,["atk"]=104,["def"]=78,["spa"]=159,["spd"]=115,["spe"]=100 },
            ["abilities"] = new JsonArray { "ひでり" },
        },
        ["raichu"] = new JsonObject
        {
            ["num"] = 26,
            ["name"] = "ライチュウ",
            ["types"] = new JsonArray { "Electric" },
            ["baseStats"] = new JsonObject { ["hp"]=60,["atk"]=90,["def"]=55,["spa"]=90,["spd"]=80,["spe"]=110 },
            ["abilities"] = new JsonArray { "せいでんき", "ひらいしん" },
        },
    };

    // megaStone フィールドを持つアイテムを含む showdown-items のフラット表現
    private static JsonObject MakeShowdownItemsWithMegaStones() => new()
    {
        ["venusaurite"] = new JsonObject
        {
            ["num"] = 659,
            ["name"] = "Venusaurite",
            ["megaStone"] = new JsonObject { ["Venusaur"] = "Venusaur-Mega" },
        },
        ["charizarditex"] = new JsonObject
        {
            ["num"] = 660,
            ["name"] = "Charizardite X",
            ["megaStone"] = new JsonObject { ["Charizard"] = "Charizard-Mega-X" },
        },
        ["charizarditey"] = new JsonObject
        {
            ["num"] = 661,
            ["name"] = "Charizardite Y",
            ["megaStone"] = new JsonObject { ["Charizard"] = "Charizard-Mega-Y" },
        },
    };

    private static JsonObject MakeItemNamesForMegas() => new()
    {
        ["venusaurite"] = "フシギバナイト",
        // 半角 X/Y の翻訳が来るケースを再現
        ["charizarditex"] = "リザードナイトX",
        ["charizarditey"] = "リザードナイトY",
    };

    [Fact]
    public void NestMegaForms_NestsMegaIntoParent_WithKeyAndItem()
    {
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        var megaForms = result["venusaur"]!["megaForms"]!.AsArray();
        Assert.Single(megaForms);
        Assert.Equal("venusaurmega", megaForms[0]!["key"]!.GetValue<string>());
        Assert.Equal("フシギバナイト", megaForms[0]!["item"]!.GetValue<string>());
        Assert.Equal("メガフシギバナ", megaForms[0]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void NestMegaForms_RemovesMegaFromTopLevel()
    {
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        Assert.False(result.ContainsKey("venusaurmega"));
        Assert.False(result.ContainsKey("charizardmegax"));
        Assert.False(result.ContainsKey("charizardmegay"));
    }

    [Fact]
    public void NestMegaForms_DualForm_NestsMultipleMegasIntoSameParent()
    {
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        var charizardMegas = result["charizard"]!["megaForms"]!.AsArray();
        Assert.Equal(2, charizardMegas.Count);
        var keys = charizardMegas.Select(m => m!["key"]!.GetValue<string>()).ToList();
        Assert.Contains("charizardmegax", keys);
        Assert.Contains("charizardmegay", keys);
    }

    [Fact]
    public void NestMegaForms_NormalizesHalfWidthXYToFullWidth_OnMegaName()
    {
        // 入力 fixture では charizardmegax/y の name を意図的に半角 X/Y で記述している。
        // X 側・Y 側の両方で正規化が動くことを確認し、D-8 の対称性を担保する。
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        var charizardMegas = result["charizard"]!["megaForms"]!.AsArray();
        var megaX = charizardMegas.First(m => m!["key"]!.GetValue<string>() == "charizardmegax")!;
        var megaY = charizardMegas.First(m => m!["key"]!.GetValue<string>() == "charizardmegay")!;
        Assert.Equal("メガリザードンＸ", megaX["name"]!.GetValue<string>());
        Assert.Equal("メガリザードンＹ", megaY["name"]!.GetValue<string>());
        Assert.DoesNotContain("X", megaX["name"]!.GetValue<string>());
        Assert.DoesNotContain("Y", megaY["name"]!.GetValue<string>());
    }

    [Fact]
    public void NestMegaForms_NormalizesHalfWidthXYToFullWidth_OnItemName()
    {
        // 入力 fixture では itemNames に "リザードナイトX" / "リザードナイトY" (半角) を渡す
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        var charizardMegas = result["charizard"]!["megaForms"]!.AsArray();
        var megaX = charizardMegas.First(m => m!["key"]!.GetValue<string>() == "charizardmegax")!;
        var megaY = charizardMegas.First(m => m!["key"]!.GetValue<string>() == "charizardmegay")!;
        Assert.Equal("リザードナイトＸ", megaX["item"]!.GetValue<string>());
        Assert.Equal("リザードナイトＹ", megaY["item"]!.GetValue<string>());
    }

    [Fact]
    public void NestMegaForms_PreservesMegaTypesAndBaseStatsAndAbilities()
    {
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        var venusaurMega = result["venusaur"]!["megaForms"]!.AsArray()[0]!;
        Assert.Equal(100, venusaurMega["baseStats"]!["atk"]!.GetValue<int>());
        Assert.Equal(123, venusaurMega["baseStats"]!["def"]!.GetValue<int>());
        Assert.Equal("あついしぼう", venusaurMega["abilities"]!.AsArray()[0]!.GetValue<string>());
        var types = venusaurMega["types"]!.AsArray().Select(t => t!.GetValue<string>()).ToList();
        Assert.Equal(new[] { "Grass", "Poison" }, types);
    }

    [Fact]
    public void NestMegaForms_NonMegaPokemon_RemainsUntouched()
    {
        var flat = MakeFlatPokedexWithMega();
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        // ライチュウは itemNames に対応するメガストーンが無いため megaForms を持たない
        Assert.True(result.ContainsKey("raichu"));
        Assert.Null(result["raichu"]!["megaForms"]);
    }

    [Fact]
    public void NestMegaForms_MegaWithoutEntryInPokedex_Skipped()
    {
        // mega が pokedex 側に存在しないケース (例: gyaradosmega を items に登録するが pokedex にエントリ無し)
        var flat = MakeFlatPokedexWithMega();
        var items = new JsonObject
        {
            ["gyaradosite"] = new JsonObject
            {
                ["num"] = 676,
                ["name"] = "Gyaradosite",
                ["megaStone"] = new JsonObject { ["Gyarados"] = "Gyarados-Mega" },
            },
        };
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), items, MakeItemNamesForMegas(), new JsonObject());

        // 親 gyarados も無いし、メガ独立エントリも無いので何も追加されない
        Assert.False(result.ContainsKey("gyarados"));
        Assert.False(result.ContainsKey("gyaradosmega"));
    }

    [Fact]
    public void NestMegaForms_ItemNamePatch_OverridesTranslation()
    {
        var flat = MakeFlatPokedexWithMega();
        var patch = new JsonObject { ["venusaurite"] = "パッチ済みフシギバナイト" };
        var result = MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), patch);

        var venusaurMega = result["venusaur"]!["megaForms"]!.AsArray()[0]!;
        Assert.Equal("パッチ済みフシギバナイト", venusaurMega["item"]!.GetValue<string>());
    }

    [Fact]
    public void NestMegaForms_DoesNotMutateInput()
    {
        var flat = MakeFlatPokedexWithMega();
        var beforeCount = flat.Count;
        Assert.True(flat.ContainsKey("venusaurmega"));

        MergeConverter.NestMegaForms(flat, new JsonObject(), MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        // 入力 flatPokedex は変更されない（ディープクローンして作業）
        Assert.Equal(beforeCount, flat.Count);
        Assert.True(flat.ContainsKey("venusaurmega"));
        Assert.Null(flat["venusaur"]!["megaForms"]);
    }

    [Fact]
    public void NestMegaForms_ItemlessMega_NestedWithNullItem()
    {
        // ItemlessMegas に登録されたメガ (現状レックウザのみ) は、megaStone を持たなくても
        // 孤立メガ除去の対象外となり、親エントリの megaForms[] に item: null でネストされる (D-4)。
        var flat = MakeFlatPokedexWithMega();
        flat["rayquaza"] = new JsonObject
        {
            ["num"] = 384,
            ["name"] = "レックウザ",
            ["types"] = new JsonArray { "Dragon", "Flying" },
            ["baseStats"] = new JsonObject { ["hp"]=105,["atk"]=150,["def"]=90,["spa"]=150,["spd"]=90,["spe"]=95 },
            ["abilities"] = new JsonArray { "エアロック" },
        };
        flat["rayquazamega"] = new JsonObject
        {
            ["num"] = 384,
            ["name"] = "メガレックウザ",
            ["types"] = new JsonArray { "Dragon", "Flying" },
            ["baseStats"] = new JsonObject { ["hp"]=105,["atk"]=180,["def"]=100,["spa"]=180,["spd"]=100,["spe"]=115 },
            ["abilities"] = new JsonArray { "デルタストリーム" },
        };

        var showdownPokedex = new JsonObject
        {
            // forme: "Mega" を持つが ItemlessMegas に登録されているため除去せずネストする
            ["rayquazamega"] = new JsonObject { ["forme"] = "Mega" },
            ["rayquaza"] = new JsonObject(),
        };

        var result = MergeConverter.NestMegaForms(flat, showdownPokedex, MakeShowdownItemsWithMegaStones(), MakeItemNamesForMegas(), new JsonObject());

        // 親 rayquaza は残り、megaForms[] に rayquazamega が item: null でネストされる
        Assert.True(result.ContainsKey("rayquaza"));
        Assert.False(result.ContainsKey("rayquazamega"));
        var megaForms = result["rayquaza"]!["megaForms"]!.AsArray();
        Assert.Single(megaForms);
        var mega = megaForms[0]!.AsObject();
        Assert.Equal("rayquazamega", mega["key"]!.GetValue<string>());
        Assert.Equal("メガレックウザ", mega["name"]!.GetValue<string>());
        Assert.True(mega.ContainsKey("item"));  // フィールドは存在する
        Assert.Null(mega["item"]);              // 値は JSON null (JsonObject 上は null 表現)
        Assert.Equal(180, mega["baseStats"]!["atk"]!.GetValue<int>());
        Assert.Equal("デルタストリーム", mega["abilities"]!.AsArray()[0]!.GetValue<string>());
    }

    [Fact]
    public void NestMegaForms_NonMegaFormeName_NotRemoved()
    {
        // forme フィールドの値が "Mega" で始まらない場合 (例: "Galar", "Alola") は除去対象外
        var flat = new JsonObject
        {
            ["mrmimegalar"] = new JsonObject
            {
                ["num"] = 122,
                ["name"] = "バリヤード (ガラルのすがた)",
                ["types"] = new JsonArray { "Ice", "Psychic" },
                ["baseStats"] = new JsonObject { ["hp"]=50,["atk"]=65,["def"]=65,["spa"]=90,["spd"]=90,["spe"]=100 },
                ["abilities"] = new JsonArray { "バリアフリー" },
            },
        };
        var showdownPokedex = new JsonObject
        {
            ["mrmimegalar"] = new JsonObject { ["forme"] = "Galar" },
        };

        var result = MergeConverter.NestMegaForms(flat, showdownPokedex, new JsonObject(), new JsonObject(), new JsonObject());

        Assert.True(result.ContainsKey("mrmimegalar"));
    }

    [Fact]
    public void NestMegaForms_ItemlessMega_AppendedToExistingMegaForms()
    {
        // 親エントリが既に megaStone 経由で megaForms[] を持っている場合、
        // ItemlessMegas のメガはその配列の末尾に追加される（D-4 の親既存 megaForms[] 共存挙動）。
        // 現状の ItemlessMegas には rayquazamega しか無いが、仮にレックウザに別経路の megaStone が
        // 追加されたとしても安全に動くことを示す回帰テスト。
        // ItemlessMegas は parent=rayquaza にハードコードされているため、parent と mega 両方を fixture に追加する。
        var flat = MakeFlatPokedexWithMega();
        flat["rayquaza"] = new JsonObject
        {
            ["num"] = 384,
            ["name"] = "レックウザ",
            ["types"] = new JsonArray { "Dragon", "Flying" },
            ["baseStats"] = new JsonObject { ["hp"]=105,["atk"]=150,["def"]=90,["spa"]=150,["spd"]=90,["spe"]=95 },
            ["abilities"] = new JsonArray { "エアロック" },
            // 親が既に megaForms[] を持つ前提を fixture で再現
            ["megaForms"] = new JsonArray
            {
                new JsonObject
                {
                    ["key"] = "rayquazamegaalt",
                    ["name"] = "別ルートのメガ",
                    ["item"] = "ダミーストーン",
                    ["types"] = new JsonArray { "Dragon", "Flying" },
                    ["baseStats"] = new JsonObject { ["hp"]=105,["atk"]=160,["def"]=95,["spa"]=160,["spd"]=95,["spe"]=105 },
                    ["abilities"] = new JsonArray { "ダミー特性" },
                },
            },
        };
        flat["rayquazamega"] = new JsonObject
        {
            ["num"] = 384,
            ["name"] = "メガレックウザ",
            ["types"] = new JsonArray { "Dragon", "Flying" },
            ["baseStats"] = new JsonObject { ["hp"]=105,["atk"]=180,["def"]=100,["spa"]=180,["spd"]=100,["spe"]=115 },
            ["abilities"] = new JsonArray { "デルタストリーム" },
        };

        var showdownPokedex = new JsonObject
        {
            ["rayquazamega"] = new JsonObject { ["forme"] = "Mega" },
            ["rayquaza"] = new JsonObject(),
        };

        var result = MergeConverter.NestMegaForms(flat, showdownPokedex, new JsonObject(), new JsonObject(), new JsonObject());

        var megaForms = result["rayquaza"]!["megaForms"]!.AsArray();
        // 既存の 1 件 + ItemlessMegas 経由の 1 件 = 2 件
        Assert.Equal(2, megaForms.Count);
        Assert.Equal("rayquazamegaalt", megaForms[0]!["key"]!.GetValue<string>());
        Assert.Equal("rayquazamega", megaForms[1]!["key"]!.GetValue<string>());
        Assert.True(megaForms[1]!.AsObject().ContainsKey("item"));
        Assert.Null(megaForms[1]!["item"]);
    }

    [Fact]
    public void NestMegaForms_OrphanMegaWithMegaHyphenForme_AlsoRemoved()
    {
        // forme フィールドが "Mega-X" / "Mega-Y" のようなハイフン付き値でも、
        // StartsWith("Mega") の判別により孤立メガとして除去される（D-1 の不変条件維持）。
        // 例: 仮想的にメガストーンが items.ts から消えた charizardmegax は forme="Mega-X" を持つため除去される。
        var flat = new JsonObject
        {
            ["charizardmegax"] = new JsonObject
            {
                ["num"] = 6,
                ["name"] = "メガリザードンＸ",
                ["types"] = new JsonArray { "Fire", "Dragon" },
                ["baseStats"] = new JsonObject { ["hp"]=78,["atk"]=130,["def"]=111,["spa"]=130,["spd"]=85,["spe"]=100 },
                ["abilities"] = new JsonArray { "かたいツメ" },
            },
        };
        var showdownPokedex = new JsonObject
        {
            ["charizardmegax"] = new JsonObject { ["forme"] = "Mega-X" },
        };

        var result = MergeConverter.NestMegaForms(flat, showdownPokedex, new JsonObject(), new JsonObject(), new JsonObject());

        Assert.False(result.ContainsKey("charizardmegax"));
    }
}
