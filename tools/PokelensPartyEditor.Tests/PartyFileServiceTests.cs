using PokelensCore;
using PokelensCore.Models;
using PokelensPartyEditor.Services;
using Xunit;

namespace PokelensPartyEditor.Tests;

/// <summary>PartyFileService の Load / Save 各パスを担保する（接頭辞 PFS）。</summary>
public class PartyFileServiceTests
{
    private static IDisposable WithTempRepo(out string repoRoot, out string partyPath)
    {
        repoRoot = Path.Combine(Path.GetTempPath(), "PartyFS-" + Guid.NewGuid().ToString("N"));
        string dataDir = Path.Combine(repoRoot, "data");
        Directory.CreateDirectory(dataDir);
        partyPath = Path.Combine(dataDir, "party.json");
        return DataPaths.OverrideRepoRoot(repoRoot);
    }

    // PFS-001: ファイル不在
    [Fact]
    public void PFS_001_Load_FileNotFound()
    {
        using var scope = WithTempRepo(out _, out _);
        var svc = new PartyFileService();
        var result = svc.Load();
        Assert.Equal(PartyFileLoadStatus.FileNotFound, result.Status);
        Assert.Null(result.Document);
    }

    // PFS-002: JSON 構文不正
    [Fact]
    public void PFS_002_Load_InvalidJson()
    {
        using var scope = WithTempRepo(out _, out var partyPath);
        File.WriteAllText(partyPath, "{this is not valid json");
        var svc = new PartyFileService();
        var result = svc.Load();
        Assert.Equal(PartyFileLoadStatus.InvalidJson, result.Status);
    }

    // PFS-003: 正常ロード
    [Fact]
    public void PFS_003_Load_Success()
    {
        using var scope = WithTempRepo(out _, out var partyPath);
        File.WriteAllText(partyPath, """
        {
          "party": [
            {
              "species": "ピカチュウ",
              "ability": "せいでんき",
              "item": "でんきだま",
              "nature": "おくびょう",
              "abilityPoints": {"hp":0,"atk":0,"def":0,"spa":32,"spd":0,"spe":32},
              "moves": [{"name":"10まんボルト"}]
            }
          ]
        }
        """);
        var svc = new PartyFileService();
        var result = svc.Load();
        Assert.Equal(PartyFileLoadStatus.Success, result.Status);
        Assert.NotNull(result.Document);
        Assert.Single(result.Document!.Party);
        Assert.Equal("ピカチュウ", result.Document.Party[0].Species);
    }

    // PFS-004: 未知キーは無視
    [Fact]
    public void PFS_004_Load_IgnoresUnknownKeys()
    {
        using var scope = WithTempRepo(out _, out var partyPath);
        File.WriteAllText(partyPath, """
        {
          "party": [
            {
              "species": "ピカチュウ",
              "hoge": "foo",
              "nature": "おくびょう",
              "abilityPoints": {"hp":0,"atk":0,"def":0,"spa":0,"spd":0,"spe":0},
              "moves": []
            }
          ],
          "extraneous": "value"
        }
        """);
        var svc = new PartyFileService();
        var result = svc.Load();
        Assert.Equal(PartyFileLoadStatus.Success, result.Status);
        Assert.Equal("ピカチュウ", result.Document!.Party[0].Species);
    }

    // PFS-005: 型不正（species が数値）
    [Fact]
    public void PFS_005_Load_InvalidTypeReturnsInvalidJson()
    {
        // System.Text.Json は型不一致を JsonException として throw する。
        // PartyFileService は JsonException を InvalidJson として返す（実装どおり）。
        using var scope = WithTempRepo(out _, out var partyPath);
        File.WriteAllText(partyPath, """
        {
          "party": [
            {
              "species": 0,
              "nature": "おくびょう",
              "abilityPoints": {"hp":0,"atk":0,"def":0,"spa":0,"spd":0,"spe":0},
              "moves": []
            }
          ]
        }
        """);
        var svc = new PartyFileService();
        var result = svc.Load();
        Assert.True(result.Status is PartyFileLoadStatus.InvalidJson or PartyFileLoadStatus.InvalidType);
    }

    // PFS-006: 正常セーブ → ファイル生成 + 内容ラウンドトリップ
    [Fact]
    public void PFS_006_Save_RoundTrip()
    {
        using var scope = WithTempRepo(out _, out var partyPath);
        var doc = new PartyDocument
        {
            Party = new List<PokemonEntryModel>
            {
                new()
                {
                    Species = "ピカチュウ",
                    Ability = "せいでんき",
                    Item = "でんきだま",
                    Nature = "おくびょう",
                    AbilityPoints = new AbilityPointsModel { Spa = 32, Spe = 32 },
                    Moves = new List<MoveEntryModel> { new() { Name = "10まんボルト" } },
                },
            },
        };
        var svc = new PartyFileService();
        var saveResult = svc.Save(doc);
        Assert.Equal(PartyFileSaveStatus.Success, saveResult.Status);
        Assert.True(File.Exists(partyPath));

        // 同じ内容で Load してラウンドトリップ
        var loadResult = svc.Load();
        Assert.Equal(PartyFileLoadStatus.Success, loadResult.Status);
        Assert.Equal("ピカチュウ", loadResult.Document!.Party[0].Species);
        Assert.Equal(32, loadResult.Document.Party[0].AbilityPoints.Spa);
    }
}
