using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PokelensCore.Models;
using PokelensPartyEditor.Services;

namespace PokelensPartyEditor.ViewModels;

/// <summary>メインウィンドウ全体の ViewModel。6 匹分の <see cref="PokemonEntryViewModel"/>・ダーティフラグ・ロード/セーブ/クローズコマンドを管理する。</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPartyFileService _fileService;
    private readonly IDialogService _dialogService;
    private readonly IMasterDataService _masterDataService;

    public IReadOnlyList<PokemonEntryViewModel> Entries { get; }

    [ObservableProperty] private bool _isDirty;

    private bool _suppressDirty;

    public MainWindowViewModel(
        IPartyFileService fileService,
        IDialogService dialogService,
        IMasterDataService masterDataService,
        ISuggestService suggestService)
    {
        _fileService = fileService;
        _dialogService = dialogService;
        _masterDataService = masterDataService;

        Entries = Enumerable.Range(0, 6)
            .Select(_ => new PokemonEntryViewModel(suggestService, masterDataService.Snapshot))
            .ToList();

        foreach (var entry in Entries)
        {
            entry.PropertyChanged += OnEntryPropertyChanged;
            foreach (var move in entry.Moves)
            {
                move.PropertyChanged += OnEntryPropertyChanged;
            }
        }

        // 初期表示は空欄状態・IsDirty = false
        ResetAllInternal();
    }

    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressDirty) return;
        IsDirty = true;
    }

    private void ResetAllInternal()
    {
        _suppressDirty = true;
        try
        {
            foreach (var entry in Entries) entry.Reset();
            IsDirty = false;
        }
        finally
        {
            _suppressDirty = false;
        }
    }

    /// <summary>「ロード」ボタン押下時の処理。</summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        var result = _fileService.Load();
        switch (result.Status)
        {
            case PartyFileLoadStatus.FileNotFound:
                await _dialogService.ShowErrorAsync("ロードエラー", result.ErrorMessage ?? "data/party.json が見つかりません");
                return;
            case PartyFileLoadStatus.InvalidJson:
                await _dialogService.ShowErrorAsync("ロードエラー", $"JSON 形式が不正です: {result.ErrorMessage}");
                return;
            case PartyFileLoadStatus.InvalidType:
                await _dialogService.ShowErrorAsync("ロードエラー", $"不正な型が含まれます: {result.ErrorMessage}");
                return;
            case PartyFileLoadStatus.IOError:
                await _dialogService.ShowErrorAsync("ロードエラー", $"読み込みに失敗しました: {result.ErrorMessage}");
                return;
            case PartyFileLoadStatus.Success:
                ApplyDocument(result.Document!);
                return;
        }
    }

    private void ApplyDocument(PartyDocument doc)
    {
        _suppressDirty = true;
        try
        {
            // 7 匹以上は 7 匹目以降を無視
            int n = Math.Min(doc.Party.Count, Entries.Count);
            for (int i = 0; i < n; i++)
            {
                Entries[i].LoadFrom(doc.Party[i]);
            }
            // 6 匹未満は不足分を空欄に
            for (int i = n; i < Entries.Count; i++)
            {
                Entries[i].Reset();
            }
            IsDirty = false;
        }
        finally
        {
            _suppressDirty = false;
        }
    }

    /// <summary>「セーブ」ボタン押下時の処理。</summary>
    [RelayCommand]
    public async Task SaveAsync()
    {
        var errors = new List<ValidationError>();
        for (int i = 0; i < Entries.Count; i++)
        {
            Entries[i].Validate(i, errors);
        }

        if (errors.Count > 0)
        {
            string message = string.Join("\n", errors.Select(e =>
                e.SlotIndex >= 0
                    ? $"スロット{e.SlotIndex + 1}: {e.Message}"
                    : e.Message));
            await _dialogService.ShowErrorAsync("入力エラー", message);
            return;
        }

        // 未入力チェック
        bool hasUnfilled = Entries.Any(e => e.HasUnfilledFields());
        if (hasUnfilled)
        {
            bool proceed = await _dialogService.ConfirmAsync(
                "未入力確認",
                "未入力項目があります。保存を続けますか？");
            if (!proceed) return;
        }

        var doc = new PartyDocument
        {
            Party = Entries
                .Where(e => !e.IsCompletelyEmpty())
                .Select(e => e.ToModel())
                .ToList(),
        };

        var saveResult = _fileService.Save(doc);
        if (saveResult.Status == PartyFileSaveStatus.IOError)
        {
            await _dialogService.ShowErrorAsync("保存エラー", $"保存に失敗しました: {saveResult.ErrorMessage}");
            return;
        }

        IsDirty = false;
    }

    /// <summary>ウィンドウクローズ時の確認。</summary>
    /// <returns>閉じる場合 true、キャンセル（閉じない）場合 false。</returns>
    public async Task<bool> ConfirmCloseAsync()
    {
        if (!IsDirty) return true;
        return await _dialogService.ConfirmAsync(
            "未保存の変更",
            "編集中の内容を保存していません。閉じますか？");
    }
}
