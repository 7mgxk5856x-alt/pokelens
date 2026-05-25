using PokelensCore;
using PokelensCore.Models;

namespace PokelensPartyEditor.Services;

/// <summary>NameSearch をマスターデータに適用してサジェスト候補を返す実装。</summary>
public sealed class SuggestService : ISuggestService
{
    private readonly MasterDataSnapshot _snapshot;

    public SuggestService(IMasterDataService masterDataService)
    {
        _snapshot = masterDataService.Snapshot;
    }

    /// <summary>ポケモン名のサジェスト。図鑑番号順は <see cref="MasterDataSnapshot.PokemonNames"/> 構築時に確定済みのため、ここでは入力順を維持する <see cref="NameSearch.SearchNames"/> を使う。</summary>
    public IReadOnlyList<string> SuggestPokemon(string query) =>
        NameSearch.SearchNames(query, _snapshot.PokemonNames);

    public IReadOnlyList<string> SuggestAbility(string query) =>
        NameSearch.SearchNames(query, _snapshot.AbilityNames);

    public IReadOnlyList<string> SuggestItem(string query) =>
        NameSearch.SearchNames(query, _snapshot.ItemNames);

    public IReadOnlyList<string> SuggestMove(string query) =>
        NameSearch.SearchNames(query, _snapshot.MoveNames);
}
