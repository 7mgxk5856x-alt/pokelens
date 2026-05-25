using System.Text.Json;
using Xunit;
using PokelensCore.Models;

namespace PokelensCore.Tests;

/// <summary>MasterDataReader の各マスタ JSON 読み取り（並び順 + 性格補正抽出）を担保する。</summary>
public class MasterDataReaderTests
{
    private static string WriteTempJson(string fileName, string content)
    {
        string dir = Path.Combine(Path.GetTempPath(), "PokelensCore.Tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, fileName);
        File.WriteAllText(path, content);
        return path;
    }

    // MDR-001: pokedex は num 昇順で取り出す
    [Fact]
    public void MDR_001_LoadPokemonNames_SortedByNum()
    {
        string path = WriteTempJson("pokedex.json", """
        {
            "bulbasaur": { "num": 1, "name": "フシギダネ" },
            "venusaur":  { "num": 3, "name": "フシギバナ" },
            "ivysaur":   { "num": 2, "name": "フシギソウ" }
        }
        """);
        var names = MasterDataReader.LoadPokemonNames(path);
        Assert.Equal(new[] { "フシギダネ", "フシギソウ", "フシギバナ" }, names);
    }

    // MDR-002: num/name が欠落・型不正なエントリはスキップ
    [Fact]
    public void MDR_002_LoadPokemonNames_SkipsMalformedEntry()
    {
        string path = WriteTempJson("pokedex.json", """
        {
            "ok":      { "num": 1, "name": "OK" },
            "noNum":   { "name": "NoNum" },
            "noName":  { "num": 2 },
            "badType": { "num": "x", "name": "BadType" }
        }
        """);
        var names = MasterDataReader.LoadPokemonNames(path);
        Assert.Equal(new[] { "OK" }, names);
    }

    // MDR-003: LoadKeys はトップレベルキーを入力順で取り出す
    [Fact]
    public void MDR_003_LoadKeys_PreservesInsertionOrder()
    {
        string path = WriteTempJson("abilities.json", """
        {
            "あついしぼう": { "modifier": null },
            "しんりょく":   { "modifier": null },
            "もうか":       { "modifier": null }
        }
        """);
        var keys = MasterDataReader.LoadKeys(path);
        Assert.Equal(new[] { "あついしぼう", "しんりょく", "もうか" }, keys);
    }

    // MDR-004: natures から上昇/下降ステータスを抽出する
    [Fact]
    public void MDR_004_LoadNatureModifiers_Basic()
    {
        string path = WriteTempJson("natures.json", """
        {
            "いじっぱり": { "modifiers": { "atk": 1.1, "spa": 0.9 } },
            "ようき":     { "modifiers": { "spe": 1.1, "spa": 0.9 } },
            "がんばりや": { "modifiers": {} }
        }
        """);
        var mods = MasterDataReader.LoadNatureModifiers(path);
        Assert.Equal(new NatureModifiers("atk", "spa"), mods["いじっぱり"]);
        Assert.Equal(new NatureModifiers("spe", "spa"), mods["ようき"]);
        Assert.Equal(new NatureModifiers(null, null), mods["がんばりや"]);
    }

    // MDR-005: 補正値が 1.0 のフィールドは無視される（0.95〜1.05 帯は補正なし扱い）
    [Fact]
    public void MDR_005_LoadNatureModifiers_NeutralValueIgnored()
    {
        string path = WriteTempJson("natures.json", """
        {
            "test": { "modifiers": { "atk": 1.0, "def": 1.0 } }
        }
        """);
        var mods = MasterDataReader.LoadNatureModifiers(path);
        Assert.Equal(new NatureModifiers(null, null), mods["test"]);
    }

    // MDR-006: Load() は全マスタを集約する（DataPaths.OverrideRepoRoot で隔離）
    [Fact]
    public void MDR_006_Load_AggregatesAllMasters()
    {
        string dir = Path.Combine(Path.GetTempPath(), "PokelensCore.Tests-" + Guid.NewGuid().ToString("N"));
        string dataDir = Path.Combine(dir, "data");
        Directory.CreateDirectory(dataDir);

        File.WriteAllText(Path.Combine(dataDir, "pokedex.json"),
            """{ "p":{"num":1,"name":"Poke1"} }""");
        File.WriteAllText(Path.Combine(dataDir, "abilities.json"), """{ "a1":{}, "a2":{} }""");
        File.WriteAllText(Path.Combine(dataDir, "items.json"), """{ "i1":{} }""");
        File.WriteAllText(Path.Combine(dataDir, "moves.json"), """{ "m1":{} }""");
        File.WriteAllText(Path.Combine(dataDir, "natures.json"),
            """{ "n1":{"modifiers":{"atk":1.1,"def":0.9}} }""");

        using (DataPaths.OverrideRepoRoot(dir))
        {
            var snap = MasterDataReader.Load();
            Assert.Equal(new[] { "Poke1" }, snap.PokemonNames);
            Assert.Equal(new[] { "a1", "a2" }, snap.AbilityNames);
            Assert.Equal(new[] { "i1" }, snap.ItemNames);
            Assert.Equal(new[] { "m1" }, snap.MoveNames);
            Assert.Equal(new[] { "n1" }, snap.NatureNames);
            Assert.Equal(new NatureModifiers("atk", "def"), snap.NatureModifiers["n1"]);
        }
    }
}
