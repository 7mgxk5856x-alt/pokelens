namespace PokelensCore.Models;

/// <summary>マスターデータ一式のスナップショット（pokedex / abilities / items / moves / natures の名前一覧）。</summary>
/// <remarks>
/// GUI 側のサジェスト・バリデーション用途で、名前リストを保持する。
/// 各リストの並び順は呼び出し元（MasterDataReader）が確定させる:
/// - PokemonNames: 図鑑番号順（機能 3 と同じ）
/// - AbilityNames / ItemNames / MoveNames: 五十音順
/// - NatureNames: data/natures.json のキー順（性格名 + 補正併記は PokemonEntryViewModel 側で結合）
/// </remarks>
public sealed class MasterDataSnapshot
{
    public IReadOnlyList<string> PokemonNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AbilityNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ItemNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MoveNames { get; init; } = Array.Empty<string>();

    /// <summary>性格名一覧（natures.json のキー）。</summary>
    public IReadOnlyList<string> NatureNames { get; init; } = Array.Empty<string>();

    /// <summary>性格名 → 補正情報（上昇ステータス / 下降ステータス）。性格プルダウンの「いじっぱり (A↑/C↓)」表示に使う。</summary>
    public IReadOnlyDictionary<string, NatureModifiers> NatureModifiers { get; init; } =
        new Dictionary<string, NatureModifiers>();
}

/// <summary>性格 1 件分の補正情報。</summary>
/// <param name="UpStat">上昇ステータス略号（"atk" / "def" / "spa" / "spd" / "spe"）。補正なしは null</param>
/// <param name="DownStat">下降ステータス略号。補正なしは null</param>
public sealed record NatureModifiers(string? UpStat, string? DownStat);
