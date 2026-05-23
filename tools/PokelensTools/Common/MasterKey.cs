namespace PokelensTools.Common;

/// <summary>data/ 配下のフロントエンド向け成果物 JSON のオブジェクトキー名。</summary>
/// <remarks>
/// MergeConverter が書き出し、フロントエンド（src/logic/ など）がプロパティ参照で読み取るスキーマ。
/// 同名でも別スキーマ（<see cref="ShowdownKey"/>＝Showdown キャッシュ、<see cref="PokeApiKey"/>＝PokéAPI レスポンス）とは
/// 用途が異なるため独立して管理する。書き／読みは異言語をまたぐので一つの const を共有することはできないが、
/// 少なくとも C# 側のドリフトを防ぐ拠点として定数を置く。エントリ種別ごとに入れ子クラスでまとめる。
/// </remarks>
internal static class MasterKey
{
    /// <summary>pokedex.json のエントリ内キー。</summary>
    internal static class Pokedex
    {
        internal const string Num = "num";
        internal const string Name = "name";
        internal const string Types = "types";
        internal const string BaseStats = "baseStats";
        internal const string Abilities = "abilities";
    }

    /// <summary>moves.json のエントリ内キー。</summary>
    internal static class Move
    {
        internal const string Type = "type";
        internal const string Category = "category";
        internal const string Power = "power";
        internal const string Accuracy = "accuracy";
        internal const string Tags = "tags";
    }
}
