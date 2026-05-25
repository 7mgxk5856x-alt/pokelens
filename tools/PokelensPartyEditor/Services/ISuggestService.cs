namespace PokelensPartyEditor.Services;

/// <summary>テキスト入力欄に対するサジェスト候補を返す Service。</summary>
public interface ISuggestService
{
    IReadOnlyList<string> SuggestPokemon(string query);
    IReadOnlyList<string> SuggestAbility(string query);
    IReadOnlyList<string> SuggestItem(string query);
    IReadOnlyList<string> SuggestMove(string query);
}
