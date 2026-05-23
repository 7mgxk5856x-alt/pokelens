using System.Text.Json.Nodes;
using PokelensTools.Common;
using PokelensTools.Pipeline;
using Xunit;

namespace PokelensTools.Tests;

public class PipelineIntegrationTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly string _cacheDir;
    private readonly string _patchesDir;
    private readonly IDisposable _repoRootScope;

    public PipelineIntegrationTests()
    {
        // temp dir を仮のリポジトリルートとして扱い、本番ディレクトリ構造（cache/、tools/PokelensTools/Patches/、data/）を再現する。
        // DataPaths.OverrideRepoRoot で library 関数（MergeConverter / PatchApplicator）の参照先を temp dir に redirect する。
        _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _cacheDir = Path.Combine(_tmpDir, "cache");
        _patchesDir = Path.Combine(_tmpDir, "tools", "PokelensTools", "Patches");
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
}
