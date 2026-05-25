using PokelensCore.Models;
using PokelensPartyEditor.ViewModels;
using Xunit;

namespace PokelensPartyEditor.Tests;

/// <summary>PokemonEntryViewModel の補助ボタン挙動・バリデーション・モデル変換を担保する（接頭辞 PEVM）。</summary>
public class PokemonEntryViewModelTests
{
    private static PokemonEntryViewModel MakeVm(MasterDataSnapshot? snapshot = null)
    {
        var snap = snapshot ?? FakeServices.MakeSnapshot();
        return new PokemonEntryViewModel(new FakeSuggestService(snap), snap);
    }

    // PEVM-001: 補助ボタン「0」はリセットする
    [Fact]
    public void PEVM_001_ResetPoint_SetsZero()
    {
        var vm = MakeVm();
        vm.Hp = "20";
        vm.ResetPoint("hp");
        Assert.Equal("0", vm.Hp);
    }

    // PEVM-002: 補助ボタン「最大」は 32 を設定する
    [Fact]
    public void PEVM_002_MaxPoint_Sets32()
    {
        var vm = MakeVm();
        vm.MaxPoint("atk");
        Assert.Equal("32", vm.Atk);
    }

    // PEVM-003: 補助ボタン「+」は 0〜31 のとき +1（32 で打ち止め）
    [Theory]
    [InlineData("0", "1")]
    [InlineData("15", "16")]
    [InlineData("31", "32")]
    public void PEVM_003_IncrementPoint_WithinRange(string current, string expected)
    {
        var vm = MakeVm();
        vm.Hp = current;
        vm.IncrementPoint("hp");
        Assert.Equal(expected, vm.Hp);
    }

    // PEVM-004: 補助ボタン「+」は 32 のときは変更なし
    [Fact]
    public void PEVM_004_IncrementPoint_AtMaxDoesNothing()
    {
        var vm = MakeVm();
        vm.Hp = "32";
        vm.IncrementPoint("hp");
        Assert.Equal("32", vm.Hp);
    }

    // PEVM-005: 補助ボタン「-」は 1〜32 のとき -1（0 で打ち止め）
    [Theory]
    [InlineData("32", "31")]
    [InlineData("15", "14")]
    [InlineData("1", "0")]
    public void PEVM_005_DecrementPoint_WithinRange(string current, string expected)
    {
        var vm = MakeVm();
        vm.Hp = current;
        vm.DecrementPoint("hp");
        Assert.Equal(expected, vm.Hp);
    }

    // PEVM-006: 補助ボタン「-」は 0 のときは変更なし
    [Fact]
    public void PEVM_006_DecrementPoint_AtMinDoesNothing()
    {
        var vm = MakeVm();
        vm.Hp = "0";
        vm.DecrementPoint("hp");
        Assert.Equal("0", vm.Hp);
    }

    // PEVM-007: 補助ボタン「+/-」は整数以外（"abc" / "" / "1.5"）のとき変更なし
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1.5")]
    [InlineData("-5")]
    public void PEVM_007_IncrementDecrement_NonIntegerNoChange(string current)
    {
        var vm = MakeVm();
        vm.Hp = current;
        vm.IncrementPoint("hp");
        Assert.Equal(current, vm.Hp);
        vm.DecrementPoint("hp");
        Assert.Equal(current, vm.Hp);
    }

    // PEVM-008: 補助ボタン「+/-」は範囲外整数（33 / -1）のとき変更なし
    [Theory]
    [InlineData("33")]
    [InlineData("-1")]
    [InlineData("100")]
    public void PEVM_008_IncrementDecrement_OutOfRangeNoChange(string current)
    {
        var vm = MakeVm();
        vm.Hp = current;
        vm.IncrementPoint("hp");
        Assert.Equal(current, vm.Hp);
        vm.DecrementPoint("hp");
        Assert.Equal(current, vm.Hp);
    }

    // PEVM-009: Validate — 完全空欄スロットはエラーなし
    [Fact]
    public void PEVM_009_Validate_EmptyEntryNoErrors()
    {
        var vm = MakeVm();
        var errors = new List<ValidationError>();
        vm.Validate(0, errors);
        Assert.Empty(errors);
    }

    // PEVM-010: Validate — 存在しないポケモン名でエラー
    [Fact]
    public void PEVM_010_Validate_UnknownSpeciesError()
    {
        var vm = MakeVm();
        vm.Species = "存在しないポケモン";
        vm.Nature = "いじっぱり";
        var errors = new List<ValidationError>();
        vm.Validate(0, errors);
        Assert.Contains(errors, e => e.Field == "species");
    }

    // PEVM-011: Validate — 能力ポイント合計 66 超でエラー
    [Fact]
    public void PEVM_011_Validate_TotalOverLimitError()
    {
        var vm = MakeVm();
        vm.Species = "ピカチュウ";
        vm.Nature = "いじっぱり";
        // 32 * 3 = 96 > 66
        vm.Hp = "32"; vm.Atk = "32"; vm.Def = "32";
        var errors = new List<ValidationError>();
        vm.Validate(0, errors);
        Assert.Contains(errors, e => e.Field == "abilityPointsTotal");
    }

    // PEVM-012: Validate — 能力ポイントが範囲外（33）でエラー
    [Fact]
    public void PEVM_012_Validate_AbilityPointOutOfRangeError()
    {
        var vm = MakeVm();
        vm.Species = "ピカチュウ";
        vm.Nature = "いじっぱり";
        vm.Hp = "33";
        var errors = new List<ValidationError>();
        vm.Validate(0, errors);
        Assert.Contains(errors, e => e.Field == "abilityPoints");
    }

    // PEVM-013: Validate — 存在しない技でエラー、空欄技はスキップ
    [Fact]
    public void PEVM_013_Validate_MoveExistenceCheck()
    {
        var vm = MakeVm();
        vm.Species = "ピカチュウ";
        vm.Nature = "いじっぱり";
        vm.Moves[0].Name = "存在しない技";
        vm.Moves[1].Name = ""; // 空欄はスキップ
        vm.Moves[2].Name = "でんきショック"; // 既知
        var errors = new List<ValidationError>();
        vm.Validate(0, errors);
        Assert.Contains(errors, e => e.Field == "move0");
        Assert.DoesNotContain(errors, e => e.Field == "move1");
        Assert.DoesNotContain(errors, e => e.Field == "move2");
    }

    // PEVM-014: HasUnfilledFields — 完全空欄は false
    [Fact]
    public void PEVM_014_HasUnfilledFields_EmptyIsFalse()
    {
        var vm = MakeVm();
        Assert.False(vm.HasUnfilledFields());
    }

    // PEVM-015: HasUnfilledFields — 一部入力ありかつ空欄技ありは true
    [Fact]
    public void PEVM_015_HasUnfilledFields_PartiallyFilledIsTrue()
    {
        var vm = MakeVm();
        vm.Species = "ピカチュウ";
        // ability / item / nature / moves が空欄のまま
        Assert.True(vm.HasUnfilledFields());
    }

    // PEVM-016: ToModel ↔ LoadFrom のラウンドトリップ
    [Fact]
    public void PEVM_016_ToModelLoadFromRoundTrip()
    {
        var vm = MakeVm();
        vm.Species = "ピカチュウ";
        vm.Ability = "せいでんき";
        vm.Item = "でんきだま";
        vm.Nature = "いじっぱり";
        vm.Hp = "10"; vm.Atk = "20"; vm.Def = "0"; vm.Spa = "32"; vm.Spd = "0"; vm.Spe = "4";
        vm.Moves[0].Name = "でんきショック";

        var model = vm.ToModel();

        var vm2 = MakeVm();
        vm2.LoadFrom(model);
        Assert.Equal(vm.Species, vm2.Species);
        Assert.Equal(vm.Ability, vm2.Ability);
        Assert.Equal(vm.Item, vm2.Item);
        Assert.Equal(vm.Nature, vm2.Nature);
        Assert.Equal(vm.Hp, vm2.Hp);
        Assert.Equal(vm.Spa, vm2.Spa);
        Assert.Equal("でんきショック", vm2.Moves[0].Name);
    }

    // PEVM-017: 性格プルダウン候補が補正情報付きラベルで生成される
    [Fact]
    public void PEVM_017_NatureChoices_LabelHasModifiers()
    {
        var vm = MakeVm();
        var ijippari = vm.NatureChoices.First(c => c.Key == "いじっぱり");
        Assert.Contains("A↑", ijippari.DisplayLabel);
        Assert.Contains("C↓", ijippari.DisplayLabel);

        var ganbariya = vm.NatureChoices.First(c => c.Key == "がんばりや");
        Assert.Contains("補正なし", ganbariya.DisplayLabel);
    }
}
