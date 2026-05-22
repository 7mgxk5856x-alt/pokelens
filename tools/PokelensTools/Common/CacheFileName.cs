namespace PokelensTools;

/// <summary>cache/ 配下の中間データファイル名。書き込み側（fetcher）と読み込み側（Program）で共有する。</summary>
/// <remarks>
/// 同じ名前を複数箇所に直書きすると、片方を変えたときに差分判定が存在しないファイルのハッシュ（空文字）を
/// 計算してしまい静かに壊れる。定数として一元化し、書き／読みのドリフトを防ぐ。
/// </remarks>
internal static class CacheFileName
{
    internal const string ShowdownPokedex = "showdown-pokedex.json";
    internal const string ShowdownMoves = "showdown-moves.json";
    internal const string ShowdownItems = "showdown-items.json";
    internal const string ShowdownAbilities = "showdown-abilities.json";
    internal const string PokeApiTranslations = "pokeapi-translations.json";
}
