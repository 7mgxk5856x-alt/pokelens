using PokelensTools.Common;
using PokelensTools.Models;
using PokelensTools.Pipeline;
using Xunit;

namespace PokelensTools.Tests;

public class IncrementalRunnerTests
{
    [Fact]
    public void DetermineSteps_FirstRun_AllStepsRequired()
    {
        ChecksumSet? previous = null;
        var current = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");

        var steps = IncrementalRunner.DetermineSteps(previous, current);

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
    public void LoadChecksums_NonExistentFile_ReturnsNull()
    {
        // RepoRoot を空の temp dir に redirect すれば、cache/checksums.json は存在しない状態になる
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            using var _ = DataPaths.OverrideRepoRoot(tmpDir);
            Assert.Null(IncrementalRunner.LoadChecksums());
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    [Fact]
    public void ComputeHash_NonExistentFile_ReturnsEmptyString()
    {
        // ComputeHash は任意のファイルパスを受ける汎用ユーティリティ。DataPaths への依存はない。
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Assert.Equal(string.Empty, IncrementalRunner.ComputeHash(path));
    }

    [Fact]
    public void SaveAndLoadChecksums_RoundTrip()
    {
        // temp dir を RepoRoot として扱い、SaveChecksums → LoadChecksums を同じ場所で round-trip させる
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            using var _ = DataPaths.OverrideRepoRoot(tmpDir);
            var checksums = MakeChecksums("a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k");

            IncrementalRunner.SaveChecksums(checksums);
            var loaded = IncrementalRunner.LoadChecksums();

            Assert.Equal(checksums, loaded);
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
    }

    private static ChecksumSet MakeChecksums(
        string pokedex, string moves, string items, string abilities,
        string pokeapi, string champions, string movesPower, string itemsMod, string abilitiesMod,
        string pokemonNamePatch, string itemNamePatch)
    {
        return new ChecksumSet(
            ShowdownPokedex: pokedex,
            ShowdownMoves: moves,
            ShowdownItems: items,
            ShowdownAbilities: abilities,
            PokeApiTranslations: pokeapi,
            ChampionsPatch: champions,
            MovesPowerPatch: movesPower,
            ItemsModifiers: itemsMod,
            AbilitiesModifiers: abilitiesMod,
            PokemonNamePatch: pokemonNamePatch,
            ItemNamePatch: itemNamePatch);
    }
}
