namespace PokelensTools.Common;

/// <summary>moves.json の tags 配列に書き込むタグ値（区分値）。</summary>
/// <remarks>
/// MergeConverter が書き、フロントエンド（src/logic/resolve-modifier.js）が文字列リスト照合で読み取る。
/// 書き／読みで同じ値を共有するため、片方の表記を変えるとサイレントに壊れる。定数で一元化する。
/// ここに置くのは C# 側で明示的に emit する固定タグのみ。Showdown の他の flag（contact / punch / bite 等）は
/// MergeConverter.FlagToTag の汎用ルール（"is" + 先頭大文字化）で動的生成するため、リテラルとしては存在せず対象外。
/// </remarks>
internal static class MasterTag
{
    /// <summary>Showdown の "slicing" flag に対応するタグ。FlagToTag の汎用ルールでは isSlicing になってしまうため、例外対応として明示する。</summary>
    internal const string IsSlice = "isSlice";

    /// <summary>反動を持つ技であることを示すタグ（Showdown の recoil フィールドの存在で付与）。</summary>
    internal const string IsRecoil = "isRecoil";

    /// <summary>追加効果（状態異常・能力変化など）を持つ技であることを示すタグ（secondary / secondaries フィールドの存在で付与）。</summary>
    internal const string HasSecondary = "hasSecondary";
}
