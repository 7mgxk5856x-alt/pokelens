namespace PokelensTools.Pipeline;

/// <summary>tools/PokelensTools/Patches/ 配下のパッチ JSON のオブジェクトキー名。</summary>
/// <remarks>
/// 手動管理パッチ（champions-patch / moves-power-patch 等）の構造セクション・フィールドキーを一元化する。
/// パッチが上書き対象とする Showdown フィールド（baseStats / types / basePower 等）は同名でも別スキーマ扱いで
/// <see cref="PokelensTools.Fetchers.ShowdownKey"/> 側を使う（PatchApplicator はそちらを参照済み）。ここでは patch ファイル固有の
/// 構造キーのみを扱う。エントリ自体のキー（技や持ち物の slug 文字列）は動的値なので対象外。
/// </remarks>
internal static class PatchKey
{
    /// <summary>champions-patch.json のトップレベル構造。Pokémon Champions 独自データを Showdown キャッシュへマージする際の section 分け。</summary>
    /// <remarks>各セクション内のフィールドキー（baseStats / types / abilities / basePower 等）は <see cref="PokelensTools.Fetchers.ShowdownKey"/> を使う。</remarks>
    internal static class Champions
    {
        internal const string Pokedex = "pokedex";
        internal const string Moves = "moves";
    }

    /// <summary>moves-power-patch.json のエントリフィールド。威力不定技（multihit など）の最大威力を補完するためのもの。</summary>
    /// <remarks>エントリのキー（技 slug）は動的値なので対象外。</remarks>
    internal static class MovesPower
    {
        internal const string Power = "power";
    }
}
