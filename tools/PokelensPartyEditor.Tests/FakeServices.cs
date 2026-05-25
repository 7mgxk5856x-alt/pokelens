using PokelensCore;
using PokelensCore.Models;
using PokelensPartyEditor.Services;

namespace PokelensPartyEditor.Tests;

/// <summary>テスト用のフェイク Service 群。手書きフェイクで DI コンテナを使わない方針（development-guidelines に従う）。</summary>
internal static class FakeServices
{
    public static MasterDataSnapshot MakeSnapshot(
        IEnumerable<string>? pokemons = null,
        IEnumerable<string>? abilities = null,
        IEnumerable<string>? items = null,
        IEnumerable<string>? moves = null,
        IEnumerable<string>? natures = null) =>
        new()
        {
            PokemonNames = (pokemons ?? new[] { "ピカチュウ", "ガブリアス" }).ToList(),
            AbilityNames = (abilities ?? new[] { "せいでんき", "さめはだ" }).ToList(),
            ItemNames = (items ?? new[] { "でんきだま", "こだわりスカーフ" }).ToList(),
            MoveNames = (moves ?? new[] { "でんきショック", "じしん" }).ToList(),
            NatureNames = (natures ?? new[] { "いじっぱり", "がんばりや" }).ToList(),
            NatureModifiers = new Dictionary<string, NatureModifiers>
            {
                ["いじっぱり"] = new("atk", "spa"),
                ["がんばりや"] = new(null, null),
            },
        };
}

internal sealed class FakeMasterDataService : IMasterDataService
{
    public MasterDataSnapshot Snapshot { get; }
    public FakeMasterDataService(MasterDataSnapshot snapshot) => Snapshot = snapshot;
}

internal sealed class FakeSuggestService : ISuggestService
{
    private readonly MasterDataSnapshot _snapshot;
    public FakeSuggestService(MasterDataSnapshot snapshot) => _snapshot = snapshot;

    public IReadOnlyList<string> SuggestPokemon(string query) =>
        NameSearch.SearchNames(query, _snapshot.PokemonNames);
    public IReadOnlyList<string> SuggestAbility(string query) =>
        NameSearch.SearchNames(query, _snapshot.AbilityNames);
    public IReadOnlyList<string> SuggestItem(string query) =>
        NameSearch.SearchNames(query, _snapshot.ItemNames);
    public IReadOnlyList<string> SuggestMove(string query) =>
        NameSearch.SearchNames(query, _snapshot.MoveNames);
}

/// <summary>テスト用 FakePartyFileService。Load の挙動と Save の呼び出しを記録する。</summary>
internal sealed class FakePartyFileService : IPartyFileService
{
    public Func<PartyFileLoadResult>? LoadHandler { get; set; }
    public List<PartyDocument> SavedDocuments { get; } = new();
    public PartyFileSaveResult SaveResult { get; set; } = new(PartyFileSaveStatus.Success, null);

    public PartyFileLoadResult Load() =>
        LoadHandler?.Invoke() ?? new PartyFileLoadResult(PartyFileLoadStatus.Success, new PartyDocument(), null);

    public PartyFileSaveResult Save(PartyDocument document)
    {
        SavedDocuments.Add(document);
        return SaveResult;
    }
}

/// <summary>ダイアログ呼び出しを記録するフェイク。確認は事前設定の <see cref="ConfirmResult"/> で返す。</summary>
internal sealed class FakeDialogService : IDialogService
{
    public List<(string Title, string Message)> Errors { get; } = new();
    public List<(string Title, string Message)> Confirmations { get; } = new();
    public bool ConfirmResult { get; set; } = true;

    public Task ShowErrorAsync(string title, string message)
    {
        Errors.Add((title, message));
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        Confirmations.Add((title, message));
        return Task.FromResult(ConfirmResult);
    }
}
