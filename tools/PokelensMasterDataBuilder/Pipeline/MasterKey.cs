namespace PokelensMasterDataBuilder.Pipeline;

/// <summary>data/ 配下のフロントエンド向け成果物 JSON のオブジェクトキー名。</summary>
/// <remarks>
/// MergeConverter が書き出し、フロントエンド（src/logic/ など）がプロパティ参照で読み取るスキーマ。
/// 同名でも別スキーマ（<see cref="PokelensMasterDataBuilder.Fetchers.ShowdownKey"/>＝Showdown キャッシュ、<see cref="PokelensMasterDataBuilder.Fetchers.PokeApiKey"/>＝PokéAPI レスポンス）とは
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

        /// <summary>メガシンカ可能なポケモンが持つメガフォーム配列フィールド（機能 7）。</summary>
        /// <remarks>配列要素のスキーマは <see cref="MegaForm"/> 参照。</remarks>
        internal const string MegaForms = "megaForms";
    }

    /// <summary>pokedex.json の megaForms 配列要素のキー（機能 7）。</summary>
    /// <remarks>
    /// 親と同じ <see cref="Pokedex.Name"/> / <see cref="Pokedex.Types"/> / <see cref="Pokedex.BaseStats"/> /
    /// <see cref="Pokedex.Abilities"/> に加え、メガ独立識別子 <see cref="Key"/> と対応メガストーン名 <see cref="Item"/> を持つ。
    /// </remarks>
    internal static class MegaForm
    {
        /// <summary>メガフォームの内部キー（旧トップレベルキー、例: "venusaurmega", "charizardmegax"）。</summary>
        internal const string Key = "key";

        /// <summary>
        /// 対応するメガストーンの日本語名（X/Y は全角 Ｘ/Ｙ。例: "フシギバナイト", "リザードナイトＸ"）。
        /// メガストーン不要メガ（現状はメガレックウザのみ）は JSON null で格納する（D-1）。
        /// </summary>
        internal const string Item = "item";
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
