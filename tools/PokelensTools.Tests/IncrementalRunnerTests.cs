using PokelensTools;
using Xunit;

namespace PokelensTools.Tests;

public class IncrementalRunnerTests
{
    [Fact]
    public void DetermineSteps_FirstRun_AllStepsRequired()
    {
        var old = new Dictionary<string, string>();
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.True(steps.NeedsStep2);
        Assert.True(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_ShowdownChanged_Step234Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("X", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k"); // pokedex changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.True(steps.NeedsStep2);
        Assert.True(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_ChampionsPatchOnly_Step34Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "e", "X", "g", "h", "i", "j", "k"); // champions-patch changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.True(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_MovesPowerPatchOnly_Step4Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "X", "h", "i", "j", "k"); // moves-power-patch changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_ItemsModifiersOnly_Step4Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "X", "i", "j", "k"); // items-modifiers changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_AbilitiesModifiersOnly_Step4Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "X", "j", "k"); // abilities-modifiers changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_PokemonNamePatchOnly_Step4Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "X", "k"); // pokemon-name-patch changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_ItemNamePatchOnly_Step4Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "X"); // item-name-patch changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_PokeapiTranslationsOnly_Step4Required()
    {
        var old = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");
        var current = MakeChecksums("a", "b", "c", "d", "X", "f", "g", "h", "i", "j", "k"); // pokeapi-translations changed

        var steps = IncrementalRunner.DetermineSteps(old, current);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.True(steps.NeedsStep4);
    }

    [Fact]
    public void DetermineSteps_NoChanges_NoStepsRequired()
    {
        var checksums = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");

        var steps = IncrementalRunner.DetermineSteps(checksums, checksums);

        Assert.False(steps.NeedsStep2);
        Assert.False(steps.NeedsStep3);
        Assert.False(steps.NeedsStep4);
    }

    [Fact]
    public void LoadChecksums_NonExistentFile_ReturnsEmptyDictionary()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        var result = IncrementalRunner.LoadChecksums(path);
        Assert.Empty(result);
    }

    [Fact]
    public void ComputeHash_NonExistentFile_ReturnsEmptyString()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.Equal(string.Empty, IncrementalRunner.ComputeHash(path));
    }

    [Fact]
    public void SaveAndLoadChecksums_RoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".json");
        var checksums = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");

        IncrementalRunner.SaveChecksums(checksums, path);
        var loaded = IncrementalRunner.LoadChecksums(path);

        Assert.Equal(checksums, loaded);
        File.Delete(path);
    }

    private static Dictionary<string, string> MakeChecksums(
        string pokedex, string moves, string items, string abilities,
        string pokeapi, string champions, string movesPower, string itemsMod, string abilitiesMod,
        string pokemonNamePatch, string itemNamePatch)
    {
        return new Dictionary<string, string>
        {
            ["showdown-pokedex"]     = pokedex,
            ["showdown-moves"]       = moves,
            ["showdown-items"]       = items,
            ["showdown-abilities"]   = abilities,
            ["pokeapi-translations"] = pokeapi,
            ["champions-patch"]      = champions,
            ["moves-power-patch"]    = movesPower,
            ["items-modifiers"]      = itemsMod,
            ["abilities-modifiers"]  = abilitiesMod,
            ["pokemon-name-patch"]   = pokemonNamePatch,
            ["item-name-patch"]      = itemNamePatch,
        };
    }
}
