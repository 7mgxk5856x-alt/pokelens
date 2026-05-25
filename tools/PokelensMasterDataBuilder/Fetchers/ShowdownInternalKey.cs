namespace PokelensMasterDataBuilder.Fetchers;

/// <summary>Showdown の表示名を pokedex.json のトップレベル内部キーへ変換する。</summary>
/// <remarks>
/// pokedex.json のキー（例: "venusaurmega", "absolmega", "charizardmegax"）は Showdown の表示名（例: "Venusaur-Mega", "Absol-Mega", "Charizard-Mega-X"）から
/// 小文字化 + ハイフン除去 + 約物除去で導出される。一方 <see cref="PokeApiSlug"/> は PokéAPI 検索用に ハイフン区切り（例: "venusaur-mega"）を維持するため用途が異なる。
/// メガシンカリファクタ（機能 7、P0.5）で items.ts の megaStone フィールド（"親英語名 → メガ英語名"）から
/// pokedex.json のキーへ変換する目的で導入した。
/// </remarks>
internal static class ShowdownInternalKey
{
    /// <summary>Showdown のポケモン表示名を pokedex.json のトップレベル内部キーに変換する。</summary>
    /// <remarks><see cref="PokeApiSlug.PokemonFormSlug"/> の結果からハイフンを除去した形に等しい（slug は "-" 区切り、内部キーは区切りなし）。</remarks>
    /// <param name="showdownName">Showdown のポケモン表示名（例: "Venusaur-Mega", "Charizard-Mega-X"）。</param>
    /// <returns>pokedex.json の内部キー（例: "venusaurmega", "charizardmegax"）。</returns>
    internal static string ForPokemon(string showdownName)
        => PokeApiSlug.PokemonFormSlug(showdownName).Replace("-", "");
}
