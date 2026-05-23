using System.Text.Json.Nodes;
using PokelensTools.Pipeline;
using Xunit;

namespace PokelensTools.Tests;

public class PipelineIntegrationTests : IDisposable
{
    private readonly string _tmpDir;

    public PipelineIntegrationTests()
    {
        _tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tmpDir);
    }

    public void Dispose()
    {
        Directory.Delete(_tmpDir, recursive: true);
    }

    private string Write(string name, string json)
    {
        var path = Path.Combine(_tmpDir, name);
        File.WriteAllText(path, json);
        return path;
    }

    [Fact]
    public void FullPipeline_ProducesCorrectPokedexJson()
    {
        var pokedexPath = Write("showdown-pokedex.json", """
            {
              "pikachu": {
                "num": 25, "name": "Pikachu",
                "types": ["Electric"],
                "baseStats": {"hp":35,"atk":55,"def":40,"spa":50,"spd":50,"spe":90},
                "abilities": {"0":"Static","H":"Lightning Rod"}
              }
            }
            """);
        var movesPath = Write("showdown-moves.json", """
            {
              "thunderbolt": {
                "num": 85, "name": "Thunderbolt", "type": "Electric", "category": "Special",
                "basePower": 90, "accuracy": 100, "flags": {"protect":1}
              }
            }
            """);
        var itemsPath = Write("showdown-items.json", """{"choicescarf":{"num":287}}""");
        var abilitiesPath = Write("showdown-abilities.json", """{"ironfist":{"num":89}}""");
        var translationsPath = Write("pokeapi-translations.json", """
            {
              "pokemon": {"pikachu":"ピカチュウ"},
              "moves": {"thunderbolt":"10まんボルト"},
              "abilities": {"ironfist":"てつのこぶし","static":"せいでんき","lightningrod":"ひらいしん"},
              "items": {"choicescarf":"こだわりスカーフ"}
            }
            """);
        var movesPowerPatchPath = Write("moves-power-patch.json", "{}");
        var itemsModifiersPath = Write("items-modifiers.json", """
            {"choicescarf":{"modifier":{"spe":1.5}}}
            """);
        var abilitiesModifiersPath = Write("abilities-modifiers.json", """
            {"ironfist":{"modifier":{"condition":"isPunch","atk":1.2}}}
            """);
        var pokemonNamePatchPath = Write("pokemon-name-patch.json", "{}");
        var itemNamePatchPath = Write("item-name-patch.json", "{}");

        var dataDir = Path.Combine(_tmpDir, "data");

        MergeConverter.Convert(
            pokedexPath, movesPath, itemsPath, abilitiesPath,
            translationsPath, movesPowerPatchPath,
            itemsModifiersPath, abilitiesModifiersPath,
            pokemonNamePatchPath, itemNamePatchPath,
            dataDir);

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
        var pokedexPath = Write("showdown-pokedex.json", """
            {
              "pikachu": {
                "num": 25, "name": "Pikachu",
                "types": ["Electric"],
                "baseStats": {"hp":35,"atk":55,"def":40,"spa":50,"spd":50,"spe":90},
                "abilities": {"0":"Static"}
              }
            }
            """);
        var patchPath = Write("champions-patch.json", """
            {"pokedex":{"pikachu":{"baseStats":{"atk":99}}}}
            """);
        var dummyMovesPath = Write("no-moves.json", "{}");

        PatchApplicator.Apply(pokedexPath, dummyMovesPath, patchPath);

        var patched = JsonNode.Parse(File.ReadAllText(pokedexPath))!.AsObject();
        Assert.Equal(99, patched["pikachu"]!["baseStats"]!["atk"]!.GetValue<int>());
        Assert.Equal(35, patched["pikachu"]!["baseStats"]!["hp"]!.GetValue<int>());
    }
}
