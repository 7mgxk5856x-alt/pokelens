namespace PokelensTools;

/// <summary>data/ 配下に書き出すフロントエンド向け成果物 JSON のファイル名。</summary>
/// <remarks>
/// MergeConverter が書き出し、フロントエンド（src/data/loader.js）が fetch して読む。
/// 書き／読みで同じファイル名を共有するため、片方を変えるとサイレントに壊れる。定数で一元化する。
/// 中間キャッシュのファイル名は別用途のため <see cref="CacheFileName"/> 側で管理する。
/// </remarks>
internal static class MasterDataFile
{
    internal const string Pokedex = "pokedex.json";
    internal const string Moves = "moves.json";
    internal const string Items = "items.json";
    internal const string Abilities = "abilities.json";
}
