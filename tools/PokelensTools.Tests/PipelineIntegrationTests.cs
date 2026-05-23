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

        var abilitiesOutput = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDir, "abilities.json")))!.AsObject();
        Assert.True(abilitiesOutput.ContainsKey("てつのこぶし"));
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
}
