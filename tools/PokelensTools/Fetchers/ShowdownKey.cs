namespace PokelensTools.Fetchers;

/// <summary>Pokémon Showdown のデータ JS および成果物 JSON のオブジェクトキー名。</summary>
/// <remarks>
/// 入力（Showdown JS）の読み取りキーと出力（cache/ の成果物 JSON）の書き込みキーは同名のため共用する。
/// 同じキー名を複数箇所に直書きすると、片方を変えたときに表記揺れ・キー名ドリフトが静かに混入する。
/// 定数として一元化し、読み／書きのドリフトを防ぐ。エントリ種別ごとに入れ子クラスでまとめる。
/// </remarks>
internal static class ShowdownKey
{
    /// <summary>全エントリ種別で共通のキー。</summary>
    internal const string Num = "num";

    /// <summary>ポケモン・技・アイテムで共通の表示名キー（特性エントリには無い）。</summary>
    internal const string Name = "name";

    /// <summary>非標準（Past / Future / CAP 由来）を示す除外フラグ。アイテム・特性で使用する。</summary>
    internal const string IsNonstandard = "isNonstandard";

    /// <summary>メガストーン専用フィールド: <c>{ "親英語名": "メガ英語名" }</c> マップ（機能 7）。</summary>
    /// <remarks>このフィールドを持つアイテムは <c>isNonstandard: "Past"</c> でも MergeConverter のメガネスト処理で必要なため、ShowdownFetcher は出力から除外しない。</remarks>
    internal const string MegaStone = "megaStone";

    /// <summary>ポケモン（pokedex）エントリ固有のキー。</summary>
    /// <remarks>共通の num / name は親クラスの定数を使う。能力値の内訳キーは <see cref="Stat"/> 側にまとめる。</remarks>
    internal static class Pokedex
    {
        internal const string Types = "types";
        internal const string BaseStats = "baseStats";
        internal const string Abilities = "abilities";
        internal const string Forme = "forme";
    }

    /// <summary>baseStats オブジェクト内の各能力値キー。</summary>
    /// <remarks><see cref="Pokedex.BaseStats"/> が指すオブジェクトの中身。Showdown の略称（spa = 特攻 など）をそのまま使う。</remarks>
    internal static class Stat
    {
        internal const string Hp = "hp";
        internal const string Atk = "atk";
        internal const string Def = "def";
        internal const string Spa = "spa";
        internal const string Spd = "spd";
        internal const string Spe = "spe";
    }

    /// <summary>技（moves）エントリ固有のキー。</summary>
    /// <remarks>共通の num / name は親クラスの定数を使う。IsZ / IsMax は現代対戦で使用不可の技を除外する判定キー。</remarks>
    internal static class Move
    {
        internal const string Type = "type";
        internal const string Category = "category";
        internal const string BasePower = "basePower";
        internal const string Accuracy = "accuracy";
        internal const string Flags = "flags";
        internal const string Multihit = "multihit";
        internal const string Recoil = "recoil";
        internal const string Secondary = "secondary";
        internal const string Secondaries = "secondaries";

        /// <summary>Z ワザを示す除外フラグ。</summary>
        internal const string IsZ = "isZ";

        /// <summary>(キョダイ)ダイマックスワザを示す除外フラグ。</summary>
        internal const string IsMax = "isMax";
    }
}
