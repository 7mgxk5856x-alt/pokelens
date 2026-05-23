namespace PokelensTools.Common;

/// <summary>pokeapi-translations.json（翻訳辞書）のトップレベル構造キー名。</summary>
/// <remarks>
/// PokeAPIFetcher が cache/ に書き出し、MergeConverter が読み込む翻訳辞書のセクションを指す。
/// 同じキーを書き／読み両側で共有するため、片方だけ変えるとサイレントに壊れる。定数で一元化する。
/// 同名でも別スキーマのキーは混ぜない（例: PokéAPI レスポンス内の <see cref="PokeApiKey.Species.Pokemon"/>
/// は varieties[].pokemon を指す別物。Showdown のキーは <see cref="ShowdownKey"/> 側で管理）。
/// 構造はフラットな 4 セクションのみのため入れ子にせず並べる。
/// </remarks>
internal static class TranslationKey
{
    /// <summary>ポケモン名の日本語辞書セクション。</summary>
    internal const string Pokemon = "pokemon";

    /// <summary>技名の日本語辞書セクション。</summary>
    internal const string Moves = "moves";

    /// <summary>特性名の日本語辞書セクション。</summary>
    internal const string Abilities = "abilities";

    /// <summary>アイテム名の日本語辞書セクション。</summary>
    internal const string Items = "items";
}
