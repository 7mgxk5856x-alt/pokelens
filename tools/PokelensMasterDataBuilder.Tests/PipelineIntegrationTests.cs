using System.Text.Json.Nodes;
using PokelensMasterDataBuilder.Common;
using PokelensMasterDataBuilder.Pipeline;
using Xunit;

namespace PokelensMasterDataBuilder.Tests;

public class PipelineIntegrationTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _cacheDir;
    private readonly string _patchesDir;
    private readonly IDisposable _repoRootScope;

    public PipelineIntegrationTests()
    {
        // temp dir を仮のリポジトリルートとして扱い、本番ディレクトリ構造（cache/、tools/PokelensMasterDataBuilder/Patches/、data/）を再現する。
        // DataPaths.OverrideRepoRoot で library 関数（MergeConverter / PatchApplicator）の参照先を temp dir に redirect する。
        _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _cacheDir = Path.Combine(_tmpDir, "cache");
        _patchesDir = Path.Combine(_tmpDir, "tools", "PokelensMasterDataBuilder", "Patches");
        Directory.CreateDirectory(_cacheDir);
        Directory.CreateDirectory(_patchesDir);
        _repoRootScope = DataPaths.OverrideRepoRoot(_tmpDir);
    }

    public void Dispose()
    {
        _repoRootScope.Dispose();
        Directory.Delete(_tmpDir, recursive: true);
    }

    private static void WriteFile(string path, string json)
    {
        File.WriteAllText(path, json);
    }

    [Fact]
    public void FullPipeline_ProducesCorrectPokedexJson()
    {
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "pikachu": {
                "num": 25, "name": "Pikachu",
                "types": ["Electric"],
                "baseStats": {"hp":35,"atk":55,"def":40,"spa":50,"spd":50,"spe":90},
                "abilities": {"0":"Static","H":"Lightning Rod"}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), """
            {
              "thunderbolt": {
                "num": 85, "name": "Thunderbolt", "type": "Electric", "category": "Special",
                "basePower": 90, "accuracy": 100, "flags": {"protect":1}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """{"choicescarf":{"num":287}}""");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), """{"ironfist":{"num":89}}""");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"pikachu":"ピカチュウ"},
              "moves": {"thunderbolt":"10まんボルト"},
              "abilities": {"ironfist":"てつのこぶし","static":"せいでんき","lightningrod":"ひらいしん"},
              "items": {"choicescarf":"こだわりスカーフ"}
            }
            """);
        WriteFile(Path.Combine(_patchesDir, "moves-power-patch.json"), "{}");
        WriteFile(Path.Combine(_patchesDir, "items-modifiers.json"), """
            {"choicescarf":{"modifier":{"spe":1.5}}}
            """);
        WriteFile(Path.Combine(_patchesDir, "abilities-modifiers.json"), """
            {"ironfist":{"modifier":{"condition":"isPunch","atk":1.2}}}
            """);
        WriteFile(Path.Combine(_patchesDir, "pokemon-name-patch.json"), "{}");
        WriteFile(Path.Combine(_patchesDir, "item-name-patch.json"), "{}");

        MergeConverter.Convert();

        var dataDir = Path.Combine(_tmpDir, "data");
        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "pokedex.json")))!.AsObject();
        Assert.True(pokedex.ContainsKey("pikachu"));
        Assert.Equal("ピカチュウ", pokedex["pikachu"]!["name"]!.GetValue<string>());
        Assert.Equal(35, pokedex["pikachu"]!["baseStats"]!["hp"]!.GetValue<int>());

        var abilities = pokedex["pikachu"]!["abilities"]!.AsArray();
        Assert.Equal(2, abilities.Count);
        Assert.Equal("せいでんき", abilities[0]!.GetValue<string>());
        Assert.Equal("ひらいしん", abilities[1]!.GetValue<string>());

        var moves = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "moves.json")))!.AsObject();
        Assert.True(moves.ContainsKey("10まんボルト"));
        Assert.Equal(90, moves["10まんボルト"]!["power"]!.GetValue<int>());
        Assert.Equal("Electric", moves["10まんボルト"]!["type"]!.GetValue<string>());

        var items = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "items.json")))!.AsObject();
        Assert.True(items.ContainsKey("こだわりスカーフ"));
        // modifier 内容も最終出力に保持されているか（火力指数・素早さ計算の前提データ整合）
        Assert.Equal(1.5, items["こだわりスカーフ"]!["modifier"]!["spe"]!.GetValue<double>());

        var abilitiesOutput = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "abilities.json")))!.AsObject();
        Assert.True(abilitiesOutput.ContainsKey("てつのこぶし"));
        Assert.Equal("isPunch", abilitiesOutput["てつのこぶし"]!["modifier"]!["condition"]!.GetValue<string>());
    }

    [Fact]
    public void FullPipeline_ChampionsPatch_OverridesBaseStats()
    {
        var pokedexPath = Path.Combine(_cacheDir, "showdown-pokedex.json");
        WriteFile(pokedexPath, """
            {
              "pikachu": {
                "num": 25, "name": "Pikachu",
                "types": ["Electric"],
                "baseStats": {"hp":35,"atk":55,"def":40,"spa":50,"spd":50,"spe":90},
                "abilities": {"0":"Static"}
              }
            }
            """);
        WriteFile(Path.Combine(_patchesDir, "champions-patch.json"), """
            {"pokedex":{"pikachu":{"baseStats":{"atk":99}}}}
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");

        PatchApplicator.Apply();

        var patched = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();
        Assert.Equal(99, patched["pikachu"]!["baseStats"]!["atk"]!.GetValue<int>());
        Assert.Equal(35, patched["pikachu"]!["baseStats"]!["hp"]!.GetValue<int>());
    }

    [Fact]
    public void FullPipeline_MovesPowerPatch_OverridesNullPower()
    {
        // basePower=0 の威力不定技を moves-power-patch で補完し、最終出力 moves.json（日本語キー）に反映されることを担保
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), """
            {
              "return": {
                "num": 216, "name": "Return", "type": "Normal", "category": "Physical",
                "basePower": 0, "accuracy": 100, "flags": {}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {"pokemon":{},"moves":{"return":"おんがえし"},"abilities":{},"items":{}}
            """);
        WriteFile(Path.Combine(_patchesDir, "moves-power-patch.json"), """
            {"return":{"power":102}}
            """);

        MergeConverter.Convert();

        var moves = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "moves.json")))!.AsObject();
        Assert.Equal(102, moves["おんがえし"]!["power"]!.GetValue<int>());
    }

    [Fact]
    public void FullPipeline_ChampionsPatch_OverridesMoveBasePower()
    {
        // champions-patch.json の moves セクションが showdown-moves.json の basePower を上書きし、他フィールドは据え置く
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), "{}");
        var movesPath = Path.Combine(_cacheDir, "showdown-moves.json");
        WriteFile(movesPath, """
            {
              "thunder": {
                "num": 87, "name": "Thunder", "type": "Electric", "category": "Special",
                "basePower": 110, "accuracy": 70, "flags": {}
              }
            }
            """);
        WriteFile(Path.Combine(_patchesDir, "champions-patch.json"), """
            {"moves":{"thunder":{"basePower":120}}}
            """);

        PatchApplicator.Apply();

        var patched = JsonNode.Parse(File.ReadAllText(movesPath))!.AsObject();
        Assert.Equal(120, patched["thunder"]!["basePower"]!.GetValue<int>());
        // 据え置きフィールド
        Assert.Equal(70, patched["thunder"]!["accuracy"]!.GetValue<int>());
        Assert.Equal("Electric", patched["thunder"]!["type"]!.GetValue<string>());
    }

    [Fact]
    public void FullPipeline_PokemonNamePatch_OverridesPokemonName()
    {
        // pokemon-name-patch.json による日本語名上書きが最終出力 pokedex.json に反映されることを担保
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "pikachu": {
                "num": 25, "name": "Pikachu",
                "types": ["Electric"],
                "baseStats": {"hp":35,"atk":55,"def":40,"spa":50,"spd":50,"spe":90},
                "abilities": {"0":"Static"}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {"pokemon":{"pikachu":"ピカチュウ"},"moves":{},"abilities":{"static":"せいでんき"},"items":{}}
            """);
        WriteFile(Path.Combine(_patchesDir, "pokemon-name-patch.json"), """
            {"pikachu":"パートナーピカチュウ"}
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        Assert.Equal("パートナーピカチュウ", pokedex["pikachu"]!["name"]!.GetValue<string>());
    }

    [Fact]
    public void FullPipeline_ItemNamePatch_SuppliesMissingItemTranslation()
    {
        // PokéAPI 翻訳辞書に持ち物が含まれない場合でも、item-name-patch.json で補完されて items.json に出力される
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """{"choicescarf":{"num":287}}""");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        // 翻訳辞書には choicescarf を含めない
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {"pokemon":{},"moves":{},"abilities":{},"items":{}}
            """);
        WriteFile(Path.Combine(_patchesDir, "items-modifiers.json"), """
            {"choicescarf":{"modifier":{"spe":1.5}}}
            """);
        WriteFile(Path.Combine(_patchesDir, "item-name-patch.json"), """
            {"choicescarf":"こだわりスカーフ"}
            """);

        MergeConverter.Convert();

        var items = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "items.json")))!.AsObject();
        Assert.True(items.ContainsKey("こだわりスカーフ"));
    }

    [Fact]
    public void FullPipeline_MultihitMove_OutputsMaxTotalPower()
    {
        // 連続技（multihit）の最大総威力 basePower×multihit[1] が最終出力 moves.json の power に反映されることをフルパイプラインで担保
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), """
            {
              "doubleslap": {
                "num": 3, "name": "Double Slap", "type": "Normal", "category": "Physical",
                "basePower": 15, "accuracy": 85, "multihit": [2, 5], "flags": {}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {"pokemon":{},"moves":{"doubleslap":"ダブルビンタ"},"abilities":{},"items":{}}
            """);

        MergeConverter.Convert();

        var moves = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "moves.json")))!.AsObject();
        Assert.Equal(75, moves["ダブルビンタ"]!["power"]!.GetValue<int>());
    }

    [Fact]
    public void FullPipeline_PokemonWithoutTranslation_IsExcludedFromOutput()
    {
        // pokeapi-translations.json に未登録のポケモンが最終出力 pokedex.json から除外されることを担保
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "pikachu": {
                "num": 25, "name": "Pikachu", "types":["Electric"],
                "baseStats":{"hp":35,"atk":55,"def":40,"spa":50,"spd":50,"spe":90},
                "abilities":{"0":"Static"}
              },
              "unknownpoke": {
                "num": 9999, "name": "UnknownPoke", "types":["Normal"],
                "baseStats":{"hp":1,"atk":1,"def":1,"spa":1,"spd":1,"spe":1},
                "abilities":{"0":"Static"}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        // 翻訳辞書には pikachu のみ登録、unknownpoke は登録しない
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {"pokemon":{"pikachu":"ピカチュウ"},"moves":{},"abilities":{"static":"せいでんき"},"items":{}}
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        Assert.True(pokedex.ContainsKey("pikachu"));
        Assert.False(pokedex.ContainsKey("unknownpoke"));
    }

    // ---------- 機能 7 メガシンカ P0.5 リファクタ統合検証 (PIT-009〜013) ----------

    [Fact]
    public void FullPipeline_MegaForms_NestedIntoParentAndTopLevelRemoved()
    {
        // PIT-009: NestMegaForms が MergeConverter.Convert() の経路に組み込まれており、
        // showdown-pokedex.json のメガ独立エントリが親の megaForms[] にネストされ、
        // トップレベルから削除されることをフルパイプライン経由で担保する（D-1）。
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "venusaur": {
                "num": 3, "name": "Venusaur", "types": ["Grass","Poison"],
                "baseStats": {"hp":80,"atk":82,"def":83,"spa":100,"spd":100,"spe":80},
                "abilities": {"0":"Overgrow","H":"Chlorophyll"}
              },
              "venusaurmega": {
                "num": 3, "name": "Venusaur-Mega", "types": ["Grass","Poison"],
                "baseStats": {"hp":80,"atk":100,"def":123,"spa":122,"spd":120,"spe":80},
                "abilities": {"0":"Thick Fat"},
                "forme": "Mega"
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """
            {
              "venusaurite": {
                "num": 659, "name": "Venusaurite",
                "megaStone": {"Venusaur":"Venusaur-Mega"}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"venusaur":"フシギバナ","venusaurmega":"メガフシギバナ"},
              "moves": {},
              "abilities": {"overgrow":"しんりょく","chlorophyll":"ようりょくそ","thickfat":"あついしぼう"},
              "items": {"venusaurite":"フシギバナイト"}
            }
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();

        // 親エントリに megaForms[] がネストされている
        Assert.True(pokedex.ContainsKey("venusaur"));
        var venusaurMegaForms = pokedex["venusaur"]!["megaForms"]!.AsArray();
        Assert.Single(venusaurMegaForms);
        Assert.Equal("venusaurmega", venusaurMegaForms[0]!["key"]!.GetValue<string>());
        Assert.Equal("メガフシギバナ", venusaurMegaForms[0]!["name"]!.GetValue<string>());
        Assert.Equal("フシギバナイト", venusaurMegaForms[0]!["item"]!.GetValue<string>());
        Assert.Equal(100, venusaurMegaForms[0]!["baseStats"]!["atk"]!.GetValue<int>());
        Assert.Equal("あついしぼう", venusaurMegaForms[0]!["abilities"]!.AsArray()[0]!.GetValue<string>());

        // メガ独立エントリは pokedex のトップレベルから削除されている
        Assert.False(pokedex.ContainsKey("venusaurmega"));
    }

    [Fact]
    public void FullPipeline_MegaForms_HalfWidthXY_NormalizedToFullWidth()
    {
        // PIT-011: D-8 の全角正規化が Convert() の経路を通って最終 pokedex.json に反映されることを担保。
        // 翻訳辞書由来の半角 X/Y を含む名前が、出力では全角 Ｘ/Ｙ に統一されることを確認する。
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "charizard": {
                "num": 6, "name": "Charizard", "types": ["Fire","Flying"],
                "baseStats": {"hp":78,"atk":84,"def":78,"spa":109,"spd":85,"spe":100},
                "abilities": {"0":"Blaze","H":"Solar Power"}
              },
              "charizardmegax": {
                "num": 6, "name": "Charizard-Mega-X", "types": ["Fire","Dragon"],
                "baseStats": {"hp":78,"atk":130,"def":111,"spa":130,"spd":85,"spe":100},
                "abilities": {"0":"Tough Claws"},
                "forme": "Mega-X"
              },
              "charizardmegay": {
                "num": 6, "name": "Charizard-Mega-Y", "types": ["Fire","Flying"],
                "baseStats": {"hp":78,"atk":104,"def":78,"spa":159,"spd":115,"spe":100},
                "abilities": {"0":"Drought"},
                "forme": "Mega-Y"
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """
            {
              "charizarditex": {"num":660,"name":"Charizardite X","megaStone":{"Charizard":"Charizard-Mega-X"}},
              "charizarditey": {"num":661,"name":"Charizardite Y","megaStone":{"Charizard":"Charizard-Mega-Y"}}
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        // 翻訳辞書を意図的に半角 X/Y で記述し、Convert() で全角に正規化されることを確認する
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"charizard":"リザードン","charizardmegax":"メガリザードンX","charizardmegay":"メガリザードンY"},
              "moves": {},
              "abilities": {"blaze":"もうか","solarpower":"サンパワー","toughclaws":"かたいツメ","drought":"ひでり"},
              "items": {"charizarditex":"リザードナイトX","charizarditey":"リザードナイトY"}
            }
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        var megas = pokedex["charizard"]!["megaForms"]!.AsArray();
        var megaX = megas.First(m => m!["key"]!.GetValue<string>() == "charizardmegax")!;
        var megaY = megas.First(m => m!["key"]!.GetValue<string>() == "charizardmegay")!;

        Assert.Equal("メガリザードンＸ", megaX["name"]!.GetValue<string>());
        Assert.Equal("メガリザードンＹ", megaY["name"]!.GetValue<string>());
        Assert.Equal("リザードナイトＸ", megaX["item"]!.GetValue<string>());
        Assert.Equal("リザードナイトＹ", megaY["item"]!.GetValue<string>());
        // 出力に半角 X/Y は含まれない
        Assert.DoesNotContain("X", megaX["name"]!.GetValue<string>());
        Assert.DoesNotContain("Y", megaY["name"]!.GetValue<string>());
        Assert.DoesNotContain("X", megaX["item"]!.GetValue<string>());
        Assert.DoesNotContain("Y", megaY["item"]!.GetValue<string>());
    }

    [Fact]
    public void FullPipeline_MultipleMegaForms_NestedAsArray()
    {
        // PIT-012: 複数メガ形態を持つポケモン（リザードン X/Y）が megaForms[] に全形態を格納することを統合検証。
        // PIT-011 と同じ入力構造を使い、配列要素数とキーの両方を検証する。
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "charizard": {
                "num": 6, "name": "Charizard", "types": ["Fire","Flying"],
                "baseStats": {"hp":78,"atk":84,"def":78,"spa":109,"spd":85,"spe":100},
                "abilities": {"0":"Blaze"}
              },
              "charizardmegax": {
                "num": 6, "name": "Charizard-Mega-X", "types": ["Fire","Dragon"],
                "baseStats": {"hp":78,"atk":130,"def":111,"spa":130,"spd":85,"spe":100},
                "abilities": {"0":"Tough Claws"}, "forme": "Mega-X"
              },
              "charizardmegay": {
                "num": 6, "name": "Charizard-Mega-Y", "types": ["Fire","Flying"],
                "baseStats": {"hp":78,"atk":104,"def":78,"spa":159,"spd":115,"spe":100},
                "abilities": {"0":"Drought"}, "forme": "Mega-Y"
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """
            {
              "charizarditex": {"num":660,"name":"Charizardite X","megaStone":{"Charizard":"Charizard-Mega-X"}},
              "charizarditey": {"num":661,"name":"Charizardite Y","megaStone":{"Charizard":"Charizard-Mega-Y"}}
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"charizard":"リザードン","charizardmegax":"メガリザードンＸ","charizardmegay":"メガリザードンＹ"},
              "moves": {},
              "abilities": {"blaze":"もうか","toughclaws":"かたいツメ","drought":"ひでり"},
              "items": {"charizarditex":"リザードナイトＸ","charizarditey":"リザードナイトＹ"}
            }
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        var megas = pokedex["charizard"]!["megaForms"]!.AsArray();
        Assert.Equal(2, megas.Count);
        var keys = megas.Select(m => m!["key"]!.GetValue<string>()).ToList();
        Assert.Contains("charizardmegax", keys);
        Assert.Contains("charizardmegay", keys);
        // メガ独立エントリは両方ともトップレベルから削除されている
        Assert.False(pokedex.ContainsKey("charizardmegax"));
        Assert.False(pokedex.ContainsKey("charizardmegay"));
    }

    [Fact]
    public void FullPipeline_ItemlessMega_NestedWithNullItem()
    {
        // PIT-013: メガストーン不要メガ（メガレックウザ、Dragon Ascent 仕様）が
        // MergeConverter.Convert() 経由で親の megaForms[] に item: null でネストされ、
        // トップレベルからは削除されることを統合検証する (D-4)。
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "rayquaza": {
                "num": 384, "name": "Rayquaza", "types": ["Dragon","Flying"],
                "baseStats": {"hp":105,"atk":150,"def":90,"spa":150,"spd":90,"spe":95},
                "abilities": {"0":"Air Lock"}
              },
              "rayquazamega": {
                "num": 384, "name": "Rayquaza-Mega", "types": ["Dragon","Flying"],
                "baseStats": {"hp":105,"atk":180,"def":100,"spa":180,"spd":100,"spe":115},
                "abilities": {"0":"Delta Stream"},
                "forme": "Mega"
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        // メガレックウザに対応するメガストーンは items.ts に存在しない（Dragon Ascent 仕様）
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"rayquaza":"レックウザ","rayquazamega":"メガレックウザ"},
              "moves": {},
              "abilities": {"airlock":"エアロック","deltastream":"デルタストリーム"},
              "items": {}
            }
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        // 通常 rayquaza は残り、megaForms[] にメガレックウザが item: null でネストされている
        Assert.True(pokedex.ContainsKey("rayquaza"));
        Assert.False(pokedex.ContainsKey("rayquazamega"));
        var megaForms = pokedex["rayquaza"]!["megaForms"]!.AsArray();
        Assert.Single(megaForms);
        var mega = megaForms[0]!.AsObject();
        Assert.Equal("rayquazamega", mega["key"]!.GetValue<string>());
        Assert.Equal("メガレックウザ", mega["name"]!.GetValue<string>());
        // item フィールドは存在し、値は JSON null
        Assert.True(mega.ContainsKey("item"));
        Assert.Null(mega["item"]);
        Assert.Equal(180, mega["baseStats"]!["atk"]!.GetValue<int>());
        Assert.Equal("デルタストリーム", mega["abilities"]!.AsArray()[0]!.GetValue<string>());
    }

    [Fact]
    public void FullPipeline_MegaOnlyWithoutParent_Skipped()
    {
        // PIT-014: メガ独立エントリのみ存在して親が pokedex に欠落しているケース（データ不整合）でも
        // 例外を throw せず、親を勝手に生成せず、メガ独立エントリも結果として残らないことを担保する。
        // MCT-031 の単体版を統合経路で検証する。
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "venusaurmega": {
                "num": 3, "name": "Venusaur-Mega", "types": ["Grass","Poison"],
                "baseStats": {"hp":80,"atk":100,"def":123,"spa":122,"spd":120,"spe":80},
                "abilities": {"0":"Thick Fat"},
                "forme": "Mega"
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """
            {
              "venusaurite": {
                "num": 659, "name": "Venusaurite",
                "megaStone": {"Venusaur":"Venusaur-Mega"}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"venusaurmega":"メガフシギバナ"},
              "moves": {},
              "abilities": {"thickfat":"あついしぼう"},
              "items": {"venusaurite":"フシギバナイト"}
            }
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        // 親 venusaur は存在しない (pokedex 入力に無いため)
        Assert.False(pokedex.ContainsKey("venusaur"));
        // メガ独立エントリも除去される（forme="Mega" の孤立メガとして除去）
        Assert.False(pokedex.ContainsKey("venusaurmega"));
    }

    [Fact]
    public void FullPipeline_SingleMegaForm_AlwaysOutputsAsArray()
    {
        // PIT-015: D-1 不変条件「単一メガでも常に配列を維持」を統合経路で担保する。
        // PIT-009 と同じ単一メガ入力で `megaForms` の型が JsonArray であることを確認する。
        WriteFile(Path.Combine(_cacheDir, "showdown-pokedex.json"), """
            {
              "venusaur": {
                "num": 3, "name": "Venusaur", "types": ["Grass","Poison"],
                "baseStats": {"hp":80,"atk":82,"def":83,"spa":100,"spd":100,"spe":80},
                "abilities": {"0":"Overgrow"}
              },
              "venusaurmega": {
                "num": 3, "name": "Venusaur-Mega", "types": ["Grass","Poison"],
                "baseStats": {"hp":80,"atk":100,"def":123,"spa":122,"spd":120,"spe":80},
                "abilities": {"0":"Thick Fat"},
                "forme": "Mega"
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-moves.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "showdown-items.json"), """
            {
              "venusaurite": {
                "num": 659, "name": "Venusaurite",
                "megaStone": {"Venusaur":"Venusaur-Mega"}
              }
            }
            """);
        WriteFile(Path.Combine(_cacheDir, "showdown-abilities.json"), "{}");
        WriteFile(Path.Combine(_cacheDir, "pokeapi-translations.json"), """
            {
              "pokemon": {"venusaur":"フシギバナ","venusaurmega":"メガフシギバナ"},
              "moves": {},
              "abilities": {"overgrow":"しんりょく","thickfat":"あついしぼう"},
              "items": {"venusaurite":"フシギバナイト"}
            }
            """);

        MergeConverter.Convert();

        var pokedex = JsonNode.Parse(File.ReadAllText(Path.Combine(_tmpDir, "data", "pokedex.json")))!.AsObject();
        var megaForms = pokedex["venusaur"]!["megaForms"];
        Assert.NotNull(megaForms);
        // 単一メガでも配列形式（JsonArray）で出力される
        Assert.IsType<JsonArray>(megaForms);
        Assert.Equal(1, megaForms.AsArray().Count);
    }
}
