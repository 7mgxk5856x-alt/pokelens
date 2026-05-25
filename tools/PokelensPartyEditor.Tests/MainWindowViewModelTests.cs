using PokelensCore.Models;
using PokelensPartyEditor.Services;
using PokelensPartyEditor.ViewModels;
using Xunit;

namespace PokelensPartyEditor.Tests;

/// <summary>MainWindowViewModel のロード/セーブ/クローズシーケンス、IsDirty 遷移を担保する（接頭辞 MWVM）。</summary>
public class MainWindowViewModelTests
{
    private static (MainWindowViewModel Vm, FakePartyFileService File, FakeDialogService Dialog) MakeVm(
        MasterDataSnapshot? snapshot = null)
    {
        var snap = snapshot ?? FakeServices.MakeSnapshot();
        var file = new FakePartyFileService();
        var dialog = new FakeDialogService();
        var master = new FakeMasterDataService(snap);
        var suggest = new FakeSuggestService(snap);
        var vm = new MainWindowViewModel(file, dialog, master, suggest);
        return (vm, file, dialog);
    }

    // MWVM-001: 起動直後は 6 匹空欄・IsDirty=false
    [Fact]
    public void MWVM_001_InitialStateIsCleanAndEmpty()
    {
        var (vm, _, _) = MakeVm();
        Assert.Equal(6, vm.Entries.Count);
        Assert.All(vm.Entries, e => Assert.True(e.IsCompletelyEmpty()));
        Assert.False(vm.IsDirty);
    }

    // MWVM-002: いずれかの入力欄が変化すると IsDirty=true になる
    [Fact]
    public void MWVM_002_AnyEntryChangeMakesDirty()
    {
        var (vm, _, _) = MakeVm();
        vm.Entries[0].Species = "ピカチュウ";
        Assert.True(vm.IsDirty);
    }

    // MWVM-003: ロード成功で IsDirty=false にリセット + 6 匹分の値を反映
    [Fact]
    public async Task MWVM_003_LoadSuccessResetsDirtyAndAppliesValues()
    {
        var (vm, file, _) = MakeVm();
        vm.Entries[0].Species = "編集中"; // 事前に IsDirty=true 化
        Assert.True(vm.IsDirty);

        file.LoadHandler = () => new PartyFileLoadResult(
            PartyFileLoadStatus.Success,
            new PartyDocument
            {
                Party = new List<PokemonEntryModel>
                {
                    new() { Species = "ピカチュウ", Nature = "いじっぱり", AbilityPoints = new() },
                },
            },
            null);

        await vm.LoadAsync();

        Assert.False(vm.IsDirty);
        Assert.Equal("ピカチュウ", vm.Entries[0].Species);
        Assert.True(vm.Entries[1].IsCompletelyEmpty()); // 6 匹未満の不足分は空欄
    }

    // MWVM-004: 7 匹以上の入力は 7 匹目以降を切り捨て
    [Fact]
    public async Task MWVM_004_LoadTruncatesBeyondSixEntries()
    {
        var (vm, file, _) = MakeVm();
        file.LoadHandler = () => new PartyFileLoadResult(
            PartyFileLoadStatus.Success,
            new PartyDocument
            {
                Party = Enumerable.Range(0, 8)
                    .Select(i => new PokemonEntryModel
                    {
                        Species = $"poke{i}",
                        Nature = "いじっぱり",
                        AbilityPoints = new(),
                    })
                    .ToList(),
            },
            null);

        await vm.LoadAsync();

        // 6 匹分のみ反映
        for (int i = 0; i < 6; i++)
        {
            Assert.Equal($"poke{i}", vm.Entries[i].Species);
        }
    }

    // MWVM-005: ロード時のファイル不在 → エラーダイアログ表示・画面状態は変更しない
    [Fact]
    public async Task MWVM_005_LoadFileNotFoundShowsErrorAndKeepsState()
    {
        var (vm, file, dialog) = MakeVm();
        vm.Entries[0].Species = "現状維持される";

        file.LoadHandler = () => new PartyFileLoadResult(
            PartyFileLoadStatus.FileNotFound, null, "ファイルなし");

        await vm.LoadAsync();

        Assert.Single(dialog.Errors);
        Assert.Equal("現状維持される", vm.Entries[0].Species);
    }

    // MWVM-006: セーブ時の不正値（合計 66 超）→ エラー表示、Save 呼ばれない
    [Fact]
    public async Task MWVM_006_SaveInvalidValuesShowsErrorAndDoesNotSave()
    {
        var (vm, file, dialog) = MakeVm();
        vm.Entries[0].Species = "ピカチュウ";
        vm.Entries[0].Nature = "いじっぱり";
        vm.Entries[0].Hp = "32";
        vm.Entries[0].Atk = "32";
        vm.Entries[0].Def = "32"; // 合計 96 > 66

        await vm.SaveAsync();

        Assert.Single(dialog.Errors);
        Assert.Empty(file.SavedDocuments);
    }

    // MWVM-007: セーブ時の未入力あり → 確認ダイアログ
    [Fact]
    public async Task MWVM_007_SaveWithUnfilledShowsConfirmation()
    {
        var (vm, file, dialog) = MakeVm();
        vm.Entries[0].Species = "ピカチュウ";
        vm.Entries[0].Nature = "いじっぱり";
        // ability / item / moves が未入力

        dialog.ConfirmResult = true; // 「はい」
        await vm.SaveAsync();

        Assert.Single(dialog.Confirmations);
        Assert.Single(file.SavedDocuments);
        Assert.False(vm.IsDirty);
    }

    // MWVM-008: セーブ時の未入力あり + 確認「いいえ」→ セーブされない
    [Fact]
    public async Task MWVM_008_SaveCanceledByUnfilledConfirmation()
    {
        var (vm, file, dialog) = MakeVm();
        vm.Entries[0].Species = "ピカチュウ";
        vm.Entries[0].Nature = "いじっぱり";

        dialog.ConfirmResult = false; // 「いいえ」
        await vm.SaveAsync();

        Assert.Single(dialog.Confirmations);
        Assert.Empty(file.SavedDocuments);
        Assert.True(vm.IsDirty); // セーブ取消なので IsDirty 維持
    }

    // MWVM-009: セーブ I/O 例外 → エラーダイアログ、IsDirty 維持
    [Fact]
    public async Task MWVM_009_SaveIOErrorShowsErrorAndKeepsDirty()
    {
        var (vm, file, dialog) = MakeVm();
        for (int i = 0; i < 6; i++)
        {
            var e = vm.Entries[i];
            e.Species = "ピカチュウ";
            e.Ability = "せいでんき";
            e.Item = "でんきだま";
            e.Nature = "いじっぱり";
            for (int m = 0; m < 4; m++) e.Moves[m].Name = "でんきショック";
        }

        file.SaveResult = new PartyFileSaveResult(PartyFileSaveStatus.IOError, "permission denied");
        await vm.SaveAsync();

        Assert.Single(dialog.Errors);
        Assert.True(vm.IsDirty);
    }

    // MWVM-010: セーブ成功 → IsDirty=false
    [Fact]
    public async Task MWVM_010_SaveSuccessResetsDirty()
    {
        var (vm, file, _) = MakeVm();
        for (int i = 0; i < 6; i++)
        {
            var e = vm.Entries[i];
            e.Species = "ピカチュウ";
            e.Ability = "せいでんき";
            e.Item = "でんきだま";
            e.Nature = "いじっぱり";
            for (int m = 0; m < 4; m++) e.Moves[m].Name = "でんきショック";
        }

        Assert.True(vm.IsDirty);
        await vm.SaveAsync();

        Assert.Single(file.SavedDocuments);
        Assert.False(vm.IsDirty);
    }

    // MWVM-011: ConfirmCloseAsync — IsDirty=false なら即 true（ダイアログ出さない）
    [Fact]
    public async Task MWVM_011_ConfirmClose_CleanStateReturnsTrue()
    {
        var (vm, _, dialog) = MakeVm();
        bool result = await vm.ConfirmCloseAsync();
        Assert.True(result);
        Assert.Empty(dialog.Confirmations);
    }

    // MWVM-012: ConfirmCloseAsync — IsDirty=true ならダイアログ表示 + 結果反映
    [Fact]
    public async Task MWVM_012_ConfirmClose_DirtyStateShowsDialog()
    {
        var (vm, _, dialog) = MakeVm();
        vm.Entries[0].Species = "編集中";
        dialog.ConfirmResult = false;
        bool result = await vm.ConfirmCloseAsync();
        Assert.False(result);
        Assert.Single(dialog.Confirmations);
    }
}
